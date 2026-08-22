using System.Text.RegularExpressions;

namespace TicketSystem.Publisher.Services;

public static partial class ProcessErrorExtractor
{
    public static string CreateMessage(string action, ProcessResult result)
    {
        var output = result.CombinedOutput;

        if (output.Contains("NETSDK1147", StringComparison.OrdinalIgnoreCase) || (output.Contains("maui-android", StringComparison.OrdinalIgnoreCase) && output.Contains("must be installed", StringComparison.OrdinalIgnoreCase)))
        {
            return "The .NET MAUI workload is missing. Close Publisher, run 'dotnet workload restore' as administrator, and try again.";
        }

        if (output.Contains("docker_engine", StringComparison.OrdinalIgnoreCase) || output.Contains("Docker daemon", StringComparison.OrdinalIgnoreCase) || output.Contains("Cannot connect to the Docker", StringComparison.OrdinalIgnoreCase))
        {
            return "Docker Desktop is not running or Publisher cannot access it.";
        }

        if (output.Contains("no devices/emulators found", StringComparison.OrdinalIgnoreCase) || output.Contains("device offline", StringComparison.OrdinalIgnoreCase))
        {
            return "The Android emulator is unavailable. Start an emulator in Visual Studio and try again.";
        }

        if (output.Contains("unauthorized", StringComparison.OrdinalIgnoreCase))
        {
            return "The Android device is not authorized. Confirm the USB debugging prompt on the device and try again.";
        }

        var errorLine = SplitLines(output).FirstOrDefault(IsRelevantError);
        if (errorLine is null)
        {
            return $"{action} failed. The process exited with code {result.ExitCode}.";
        }

        return $"{action} failed: {Shorten(errorLine)}";
    }

    private static IEnumerable<string> SplitLines(string output)
    {
        return output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).Select(line => line.Trim()).Where(line => line.Length > 0);
    }

    private static bool IsRelevantError(string line)
    {
        return ErrorCodeRegex().IsMatch(line)
            || line.Contains(": error ", StringComparison.OrdinalIgnoreCase)
            || line.StartsWith("error ", StringComparison.OrdinalIgnoreCase)
            || line.Contains("failed", StringComparison.OrdinalIgnoreCase)
            || line.Contains("exception", StringComparison.OrdinalIgnoreCase);
    }

    private static string Shorten(string value)
    {
        const int maxLength = 420;
        return value.Length <= maxLength ? value : $"{value[..maxLength]}...";
    }

    [GeneratedRegex(@"\b(?:NETSDK|NU|MSB|CS)\d{4}\b", RegexOptions.IgnoreCase)]
    private static partial Regex ErrorCodeRegex();
}
