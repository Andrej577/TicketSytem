using TicketSystem.Publisher.Models;

namespace TicketSystem.Publisher.Services;

public sealed class LocalPublishService(ProcessRunner processRunner, AndroidLauncher androidLauncher, HttpClient httpClient)
{
    private const string AndroidPackageName = "com.ticketsystem.app";
    private const string WindowsTargetFramework = "net8.0-windows10.0.19041.0";
    private static readonly TimeSpan ServiceStartupTimeout = TimeSpan.FromMinutes(2);

    public async Task RunAsync(Func<PublishProgress, Task> reportProgressAsync, CancellationToken cancellationToken)
    {
        var paths = CreatePaths(FindSolutionRoot());
        var ports = await ExecuteStepAsync(PublishStepId.Prerequisites, reportProgressAsync, () => ValidatePrerequisitesAsync(paths, cancellationToken));

        await ExecuteStepAsync(PublishStepId.Docker, reportProgressAsync, () => BuildAndStartDockerAsync(paths, ports, cancellationToken));
        await ExecuteStepAsync(PublishStepId.Web, reportProgressAsync, () => PublishAndLaunchWebAsync(paths, ports, cancellationToken));
        await ExecuteStepAsync(PublishStepId.Desktop, reportProgressAsync, () => PublishAndLaunchDesktopAsync(paths, cancellationToken));
        await ExecuteStepAsync(PublishStepId.Mobile, reportProgressAsync, () => PublishAndLaunchMobileAsync(paths, cancellationToken));
    }

    private async Task<LocalPorts> ValidatePrerequisitesAsync(PublisherPaths paths, CancellationToken cancellationToken)
    {
        EnsureFileExists(paths.ComposeFile, "Missing deploy/compose.yaml.");
        EnsureFileExists(paths.EnvironmentFile, "Missing deploy/.env. Copy deploy/.env.example to deploy/.env and enter local values.");
        EnsureFileExists(paths.WebProject, "Missing the TicketSystem.Web project.");
        EnsureFileExists(paths.MauiProject, "Missing the TicketSystem.Maui project.");

        await RunRequiredAsync("dotnet", new[] { "--version" }, paths.SolutionRoot, "Checking the .NET SDK", cancellationToken);
        var workloadResult = await RunRequiredAsync("dotnet", new[] { "workload", "list" }, paths.SolutionRoot, "Checking .NET MAUI workloads", cancellationToken);

        if (!HasAnyWorkload(workloadResult.StandardOutput, "maui", "maui-mobile", "maui-android"))
        {
            throw new PublisherException("The MAUI Android workload is missing. Close Publisher and run 'dotnet workload restore' as administrator.");
        }

        if (!HasAnyWorkload(workloadResult.StandardOutput, "maui", "maui-desktop", "maui-windows"))
        {
            throw new PublisherException("The MAUI Windows workload is missing. Close Publisher and run 'dotnet workload restore' as administrator.");
        }

        await RunRequiredAsync("docker", new[] { "version", "--format", "{{.Server.Version}}" }, paths.SolutionRoot, "Checking Docker Desktop", cancellationToken);
        await RunRequiredAsync("docker", new[] { "compose", "version" }, paths.SolutionRoot, "Checking Docker Compose", cancellationToken);

        var ports = ReadLocalPorts(paths.EnvironmentFile);
        if (ports.Api != 8081 || ports.Realtime != 8082 || ports.Web != 8180)
        {
            throw new PublisherException("The local Publisher currently expects API_PORT=8081, REALTIME_PORT=8082, and WEB_PORT=8180 in deploy/.env.");
        }

        return ports;
    }

    private async Task BuildAndStartDockerAsync(PublisherPaths paths, LocalPorts ports, CancellationToken cancellationToken)
    {
        await RunRequiredAsync("docker", new[]
        {
            "compose", "--env-file", paths.EnvironmentFile, "-f", paths.ComposeFile, "up", "-d", "database"
        }, paths.SolutionRoot, "PostgreSQL startup", cancellationToken);

        await RunRequiredAsync("docker", new[]
        {
            "compose", "--env-file", paths.EnvironmentFile, "-f", paths.ComposeFile, "up", "-d", "--build", "api"
        }, paths.SolutionRoot, "API build and startup", cancellationToken);

        await RunRequiredAsync("docker", new[]
        {
            "compose", "--env-file", paths.EnvironmentFile, "-f", paths.ComposeFile, "up", "-d", "--build", "realtime"
        }, paths.SolutionRoot, "Realtime build and startup", cancellationToken);

        await WaitForEndpointAsync($"http://localhost:{ports.Api}", "API", cancellationToken);
        await WaitForEndpointAsync($"http://localhost:{ports.Realtime}", "Realtime service", cancellationToken);
    }

    private async Task PublishWebAsync(PublisherPaths paths, CancellationToken cancellationToken)
    {
        PrepareOutputDirectory(paths.ArtifactsRoot, paths.WebOutput);
        await RunRequiredAsync("dotnet", new[]
        {
            "publish", paths.WebProject, "--configuration", "Release", "--output", paths.WebOutput, "--nologo"
        }, paths.SolutionRoot, "Web publish", cancellationToken);
    }

    private async Task PublishAndLaunchWebAsync(PublisherPaths paths, LocalPorts ports, CancellationToken cancellationToken)
    {
        await PublishWebAsync(paths, cancellationToken);

        await RunRequiredAsync("docker", new[]
        {
            "compose", "--env-file", paths.EnvironmentFile, "-f", paths.ComposeFile, "up", "-d", "--build", "web"
        }, paths.SolutionRoot, "Web build and startup", cancellationToken);

        await WaitForEndpointAsync($"http://localhost:{ports.Web}", "Web app", cancellationToken);
        processRunner.OpenUri($"http://localhost:{ports.Web}/login");
    }

    private async Task PublishDesktopAsync(PublisherPaths paths, CancellationToken cancellationToken)
    {
        PrepareOutputDirectory(paths.ArtifactsRoot, paths.DesktopOutput);
        await RunRequiredAsync("dotnet", new[]
        {
            "publish", paths.MauiProject,
            "--framework", WindowsTargetFramework,
            "--configuration", "Release",
            "--output", paths.DesktopOutput,
            "--nologo",
            "-p:RuntimeIdentifierOverride=win10-x64",
            "-p:WindowsPackageType=None",
            "-p:WindowsAppSDKSelfContained=true"
        }, paths.SolutionRoot, "Windows publish", cancellationToken);
    }

    private async Task PublishAndLaunchDesktopAsync(PublisherPaths paths, CancellationToken cancellationToken)
    {
        await PublishDesktopAsync(paths, cancellationToken);

        var desktopExecutable = Directory.EnumerateFiles(paths.DesktopOutput, "TicketSystem.Maui.exe", SearchOption.AllDirectories).FirstOrDefault()
            ?? throw new PublisherException("Windows publish completed, but TicketSystem.Maui.exe was not found.");

        processRunner.StartDetached(desktopExecutable, Array.Empty<string>(), Path.GetDirectoryName(desktopExecutable)!);
    }

    private async Task PublishMobileAsync(PublisherPaths paths, CancellationToken cancellationToken)
    {
        PrepareOutputDirectory(paths.ArtifactsRoot, paths.MobileOutput);
        await RunRequiredAsync("dotnet", new[]
        {
            "publish", paths.MauiProject,
            "--framework", "net8.0-android",
            "--configuration", "Release",
            "--output", paths.MobileOutput,
            "--nologo",
            "-p:AndroidPackageFormats=apk"
        }, paths.SolutionRoot, "Android publish", cancellationToken);
    }

    private async Task PublishAndLaunchMobileAsync(PublisherPaths paths, CancellationToken cancellationToken)
    {
        await PublishMobileAsync(paths, cancellationToken);

        var apkPath = FindAndroidPackage(paths.MobileOutput)
            ?? throw new PublisherException("Android publish completed, but the APK file was not found.");

        await androidLauncher.InstallAndLaunchAsync(apkPath, AndroidPackageName, paths.SolutionRoot, cancellationToken);
    }

    private async Task<ProcessResult> RunRequiredAsync(string fileName, IEnumerable<string> arguments, string workingDirectory, string action, CancellationToken cancellationToken)
    {
        var result = await processRunner.RunAsync(fileName, arguments, workingDirectory, cancellationToken);
        if (!result.Succeeded)
        {
            throw new PublisherException(ProcessErrorExtractor.CreateMessage(action, result));
        }

        return result;
    }

    private async Task WaitForEndpointAsync(string endpoint, string serviceName, CancellationToken cancellationToken)
    {
        var deadline = DateTimeOffset.UtcNow.Add(ServiceStartupTimeout);
        while (DateTimeOffset.UtcNow < deadline)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                using var response = await httpClient.GetAsync(endpoint, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
                return;
            }
            catch (HttpRequestException)
            {
            }
            catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
            }

            await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
        }

        throw new PublisherException($"{serviceName} did not start at {endpoint} within two minutes.");
    }

    private static async Task ExecuteStepAsync(PublishStepId step, Func<PublishProgress, Task> reportProgressAsync, Func<Task> action)
    {
        await ExecuteStepAsync(step, reportProgressAsync, async () =>
        {
            await action();
            return true;
        });
    }

    private static async Task<T> ExecuteStepAsync<T>(PublishStepId step, Func<PublishProgress, Task> reportProgressAsync, Func<Task<T>> action)
    {
        await reportProgressAsync(new PublishProgress(step, PublishStepStatus.Running));

        try
        {
            var result = await action();
            await reportProgressAsync(new PublishProgress(step, PublishStepStatus.Succeeded));
            return result;
        }
        catch (OperationCanceledException)
        {
            await reportProgressAsync(new PublishProgress(step, PublishStepStatus.Failed, "The operation was stopped."));
            throw;
        }
        catch (PublisherException exception)
        {
            await reportProgressAsync(new PublishProgress(step, PublishStepStatus.Failed, exception.Message));
            throw;
        }
        catch (Exception exception)
        {
            var message = $"Unexpected error: {exception.Message}";
            await reportProgressAsync(new PublishProgress(step, PublishStepStatus.Failed, message));
            throw new PublisherException(message, exception);
        }
    }

    private static string? FindAndroidPackage(string outputDirectory)
    {
        var packages = Directory.EnumerateFiles(outputDirectory, "*.apk", SearchOption.AllDirectories).ToArray();
        return packages.FirstOrDefault(path => path.EndsWith("-Signed.apk", StringComparison.OrdinalIgnoreCase))
            ?? packages.FirstOrDefault(path => !path.Contains("unsigned", StringComparison.OrdinalIgnoreCase))
            ?? packages.FirstOrDefault();
    }

    private static bool HasAnyWorkload(string output, params string[] workloadNames)
    {
        var installedWorkloads = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(line => line.Trim().Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault())
            .Where(value => value is not null)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        return workloadNames.Any(installedWorkloads.Contains);
    }

    private static LocalPorts ReadLocalPorts(string environmentFile)
    {
        var values = File.ReadLines(environmentFile)
            .Select(line => line.Trim())
            .Where(line => line.Length > 0 && !line.StartsWith('#') && line.Contains('='))
            .Select(line => line.Split('=', 2))
            .ToDictionary(parts => parts[0].Trim(), parts => parts[1].Trim(), StringComparer.OrdinalIgnoreCase);

        return new LocalPorts(ReadPort(values, "API_PORT", 8081), ReadPort(values, "REALTIME_PORT", 8082), ReadPort(values, "WEB_PORT", 8180));
    }

    private static int ReadPort(IReadOnlyDictionary<string, string> values, string key, int defaultValue)
    {
        if (!values.TryGetValue(key, out var value))
        {
            return defaultValue;
        }

        return int.TryParse(value, out var port) ? port : throw new PublisherException($"The value {key} in deploy/.env is not a valid port number.");
    }

    private static void PrepareOutputDirectory(string artifactsRoot, string outputDirectory)
    {
        var artifactsRootPath = Path.GetFullPath(artifactsRoot).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        var outputPath = Path.GetFullPath(outputDirectory);
        if (!outputPath.StartsWith(artifactsRootPath, StringComparison.OrdinalIgnoreCase))
        {
            throw new PublisherException("Publish output is not inside the artifacts/local folder.");
        }

        if (Directory.Exists(outputPath))
        {
            Directory.Delete(outputPath, recursive: true);
        }

        Directory.CreateDirectory(outputPath);
    }

    private static void EnsureFileExists(string path, string message)
    {
        if (!File.Exists(path))
        {
            throw new PublisherException(message);
        }
    }

    private static string FindSolutionRoot()
    {
        var candidates = new[] { AppContext.BaseDirectory, Environment.CurrentDirectory };
        foreach (var candidate in candidates.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            for (var directory = new DirectoryInfo(candidate); directory is not null; directory = directory.Parent)
            {
                if (File.Exists(Path.Combine(directory.FullName, "TicketSystem.sln")) && File.Exists(Path.Combine(directory.FullName, "deploy", "compose.yaml")))
                {
                    return directory.FullName;
                }
            }
        }

        throw new PublisherException("The TicketSystem solution root was not found. Publisher must be started from this repository or its artifacts folder.");
    }

    private static PublisherPaths CreatePaths(string solutionRoot)
    {
        var artifactsRoot = Path.Combine(solutionRoot, "artifacts", "local");
        return new PublisherPaths(
            solutionRoot,
            Path.Combine(solutionRoot, "deploy", "compose.yaml"),
            Path.Combine(solutionRoot, "deploy", ".env"),
            Path.Combine(solutionRoot, "src", "TicketSystem.Web", "TicketSystem.Web.csproj"),
            Path.Combine(solutionRoot, "src", "TicketSystem.Maui", "TicketSystem.Maui.csproj"),
            artifactsRoot,
            Path.Combine(artifactsRoot, "web"),
            Path.Combine(artifactsRoot, "desktop"),
            Path.Combine(artifactsRoot, "mobile"));
    }

    private sealed record LocalPorts(int Api, int Realtime, int Web);
    private sealed record PublisherPaths(string SolutionRoot, string ComposeFile, string EnvironmentFile, string WebProject, string MauiProject, string ArtifactsRoot, string WebOutput, string DesktopOutput, string MobileOutput);
}
