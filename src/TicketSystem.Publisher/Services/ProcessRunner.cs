using System.ComponentModel;
using System.Diagnostics;

namespace TicketSystem.Publisher.Services;

public sealed record ProcessResult(int ExitCode, string StandardOutput, string StandardError)
{
    public bool Succeeded => ExitCode == 0;
    public string CombinedOutput => string.Join(Environment.NewLine, new[] { StandardError, StandardOutput }.Where(value => !string.IsNullOrWhiteSpace(value)));
}

public sealed class ProcessRunner
{
    public async Task<ProcessResult> RunAsync(string fileName, IEnumerable<string> arguments, string workingDirectory, CancellationToken cancellationToken)
    {
        using var process = new Process
        {
            StartInfo = CreateStartInfo(fileName, arguments, workingDirectory, redirectOutput: true)
        };

        try
        {
            if (!process.Start())
            {
                throw new PublisherException($"Command '{fileName}' could not be started.");
            }
        }
        catch (Win32Exception exception)
        {
            throw new PublisherException($"Command '{fileName}' was not found or could not be started.", exception);
        }

        var standardOutputTask = process.StandardOutput.ReadToEndAsync();
        var standardErrorTask = process.StandardError.ReadToEndAsync();

        try
        {
            await process.WaitForExitAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
            }

            await process.WaitForExitAsync(CancellationToken.None);
            await Task.WhenAll(standardOutputTask, standardErrorTask);
            throw;
        }

        return new ProcessResult(process.ExitCode, await standardOutputTask, await standardErrorTask);
    }

    public void StartDetached(string fileName, IEnumerable<string> arguments, string workingDirectory)
    {
        var startInfo = CreateStartInfo(fileName, arguments, workingDirectory, redirectOutput: false);
        startInfo.UseShellExecute = true;

        try
        {
            Process.Start(startInfo)?.Dispose();
        }
        catch (Win32Exception exception)
        {
            throw new PublisherException($"Application '{Path.GetFileName(fileName)}' could not be started.", exception);
        }
    }

    public void StartHiddenDetached(string fileName, IEnumerable<string> arguments, string workingDirectory, IReadOnlyDictionary<string, string>? environmentVariables = null)
    {
        var startInfo = CreateStartInfo(fileName, arguments, workingDirectory, redirectOutput: false);
        ApplyEnvironmentVariables(startInfo, environmentVariables);

        try
        {
            Process.Start(startInfo)?.Dispose();
        }
        catch (Win32Exception exception)
        {
            throw new PublisherException($"Program '{Path.GetFileName(fileName)}' could not be started.", exception);
        }
    }

    public void OpenUri(string uri)
    {
        try
        {
            Process.Start(new ProcessStartInfo(uri)
            {
                UseShellExecute = true
            })?.Dispose();
        }
        catch (Win32Exception exception)
        {
            throw new PublisherException($"Web address '{uri}' could not be opened.", exception);
        }
    }

    private static ProcessStartInfo CreateStartInfo(string fileName, IEnumerable<string> arguments, string workingDirectory, bool redirectOutput)
    {
        var startInfo = new ProcessStartInfo(fileName)
        {
            WorkingDirectory = workingDirectory,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = redirectOutput,
            RedirectStandardError = redirectOutput
        };

        foreach (var argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        return startInfo;
    }

    private static void ApplyEnvironmentVariables(ProcessStartInfo startInfo, IReadOnlyDictionary<string, string>? environmentVariables)
    {
        if (environmentVariables is null)
        {
            return;
        }

        foreach (var environmentVariable in environmentVariables)
        {
            startInfo.Environment[environmentVariable.Key] = environmentVariable.Value;
        }
    }
}
