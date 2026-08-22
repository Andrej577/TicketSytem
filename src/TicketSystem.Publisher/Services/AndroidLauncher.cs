namespace TicketSystem.Publisher.Services;

public sealed class AndroidLauncher(ProcessRunner processRunner)
{
    private const string PreferredAvdName = "pixel_7_-_api_34_0";
    private static readonly TimeSpan EmulatorStartupTimeout = TimeSpan.FromMinutes(3);

    public async Task InstallAndLaunchAsync(string apkPath, string packageName, string solutionRoot, CancellationToken cancellationToken)
    {
        var androidSdkRoot = FindAndroidSdkRoot();
        var adbPath = FindExecutableOnPath("adb.exe") ?? FindSdkExecutable(androidSdkRoot, "platform-tools", "adb.exe")
            ?? throw new PublisherException("Android SDK platform-tools were not found. Install Android SDK Platform-Tools through Visual Studio Installer.");

        var deviceId = await FindOnlineDeviceAsync(adbPath, PreferredAvdName, solutionRoot, cancellationToken);
        if (deviceId is null)
        {
            deviceId = await StartEmulatorAsync(androidSdkRoot, adbPath, solutionRoot, cancellationToken);
        }

        await WaitForBootAsync(adbPath, deviceId, solutionRoot, cancellationToken);

        var installResult = await processRunner.RunAsync(adbPath, new[] { "-s", deviceId, "install", "-r", apkPath }, solutionRoot, cancellationToken);
        if (!installResult.Succeeded)
        {
            throw new PublisherException(ProcessErrorExtractor.CreateMessage("Installing Android app", installResult));
        }

        var launchResult = await processRunner.RunAsync(adbPath, new[] { "-s", deviceId, "shell", "monkey", "-p", packageName, "-c", "android.intent.category.LAUNCHER", "1" }, solutionRoot, cancellationToken);
        if (!launchResult.Succeeded)
        {
            throw new PublisherException(ProcessErrorExtractor.CreateMessage("Launching Android app", launchResult));
        }
    }

    private async Task<string> StartEmulatorAsync(string? androidSdkRoot, string adbPath, string solutionRoot, CancellationToken cancellationToken)
    {
        var emulatorPath = FindSdkExecutable(androidSdkRoot, "emulator", "emulator.exe")
            ?? throw new PublisherException("No Android device is running, and no Android emulator was found.");

        var avdHome = FindAndroidAvdHome();
        if (!File.Exists(Path.Combine(avdHome, $"{PreferredAvdName}.ini")))
        {
            throw new PublisherException("The 'Pixel 7 - API 34.0' Android emulator was not found. Create it in Visual Studio and try again.");
        }

        processRunner.StartHiddenDetached(emulatorPath, new[] { "-avd", PreferredAvdName }, Path.GetDirectoryName(emulatorPath)!, new Dictionary<string, string>
        {
            ["ANDROID_AVD_HOME"] = avdHome
        });

        var deadline = DateTimeOffset.UtcNow.Add(EmulatorStartupTimeout);
        while (DateTimeOffset.UtcNow < deadline)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var deviceId = await FindOnlineDeviceAsync(adbPath, PreferredAvdName, solutionRoot, cancellationToken);
            if (deviceId is not null)
            {
                return deviceId;
            }

            await Task.Delay(TimeSpan.FromSeconds(3), cancellationToken);
        }

        throw new PublisherException("The Android emulator did not start within three minutes.");
    }

    private async Task<string?> FindOnlineDeviceAsync(string adbPath, string avdName, string solutionRoot, CancellationToken cancellationToken)
    {
        var result = await processRunner.RunAsync(adbPath, new[] { "devices" }, solutionRoot, cancellationToken);
        if (!result.Succeeded)
        {
            throw new PublisherException(ProcessErrorExtractor.CreateMessage("Checking Android devices", result));
        }

        foreach (var line in SplitLines(result.StandardOutput).Skip(1))
        {
            var parts = line.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length >= 2 && string.Equals(parts[1], "device", StringComparison.OrdinalIgnoreCase))
            {
                var avdResult = await processRunner.RunAsync(adbPath, new[] { "-s", parts[0], "shell", "getprop", "ro.boot.qemu.avd_name" }, solutionRoot, cancellationToken);
                if (avdResult.Succeeded && string.Equals(avdResult.StandardOutput.Trim(), avdName, StringComparison.OrdinalIgnoreCase))
                {
                    return parts[0];
                }
            }
        }

        return null;
    }

    private async Task WaitForBootAsync(string adbPath, string deviceId, string solutionRoot, CancellationToken cancellationToken)
    {
        var deadline = DateTimeOffset.UtcNow.Add(EmulatorStartupTimeout);
        while (DateTimeOffset.UtcNow < deadline)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var result = await processRunner.RunAsync(adbPath, new[] { "-s", deviceId, "shell", "getprop", "sys.boot_completed" }, solutionRoot, cancellationToken);
            if (result.Succeeded && string.Equals(result.StandardOutput.Trim(), "1", StringComparison.Ordinal))
            {
                return;
            }

            await Task.Delay(TimeSpan.FromSeconds(3), cancellationToken);
        }

        throw new PublisherException("An Android device was found, but it did not finish booting within three minutes.");
    }

    private static string? FindAndroidSdkRoot()
    {
        var candidates = new[]
        {
            Environment.GetEnvironmentVariable("ANDROID_SDK_ROOT"),
            Environment.GetEnvironmentVariable("ANDROID_HOME"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Android", "Sdk")
        };

        return candidates.FirstOrDefault(path => !string.IsNullOrWhiteSpace(path) && Directory.Exists(path));
    }

    private static string FindAndroidAvdHome()
    {
        var avdHome = Environment.GetEnvironmentVariable("ANDROID_AVD_HOME");
        if (!string.IsNullOrWhiteSpace(avdHome))
        {
            return avdHome;
        }

        var androidUserHome = Environment.GetEnvironmentVariable("ANDROID_USER_HOME");
        return Path.Combine(
            string.IsNullOrWhiteSpace(androidUserHome)
                ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".android")
                : androidUserHome,
            "avd");
    }

    private static string? FindSdkExecutable(string? androidSdkRoot, params string[] parts)
    {
        if (androidSdkRoot is null)
        {
            return null;
        }

        var pathParts = new[] { androidSdkRoot }.Concat(parts).ToArray();
        var candidate = Path.Combine(pathParts);
        return File.Exists(candidate) ? candidate : null;
    }

    private static string? FindExecutableOnPath(string fileName)
    {
        var pathValue = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
        foreach (var directory in pathValue.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
        {
            var candidate = Path.Combine(directory.Trim().Trim('"'), fileName);
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        return null;
    }

    private static IEnumerable<string> SplitLines(string value)
    {
        return value.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).Select(line => line.Trim()).Where(line => line.Length > 0);
    }
}
