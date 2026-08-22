using Microsoft.AspNetCore.Components;
using MudBlazor;
using TicketSystem.Publisher.Models;
using TicketSystem.Publisher.Services;

namespace TicketSystem.Publisher.Components.Pages;

public partial class Home : IAsyncDisposable
{
    [Inject] private LocalPublishService PublishService { get; set; } = default!;

    private readonly List<PublishStepViewModel> _steps =
    [
        new(PublishStepId.Prerequisites, "Prerequisites", "Checks .NET MAUI workloads, Docker Desktop, and the local configuration."),
        new(PublishStepId.Docker, "Docker dependencies startup", "Starts PostgreSQL, then builds and starts the API and Realtime containers."),
        new(PublishStepId.Web, "Web Release publish and launch", "Publishes the Web app to artifacts/local/web and opens it for manual testing."),
        new(PublishStepId.Desktop, "Windows Release publish and launch", "Publishes the Windows MAUI app to artifacts/local/desktop and starts it for manual testing."),
        new(PublishStepId.Mobile, "Android Release publish and launch", "Publishes the Android APK to artifacts/local/mobile and installs it for manual testing.")
    ];

    private PublisherEnvironment _selectedEnvironment = PublisherEnvironment.Local;
    private CancellationTokenSource? _runCts;
    private bool _isRunning;
    private bool _completed;
    private string? _blockingError;

    private void SelectEnvironment(PublisherEnvironment environment)
    {
        if (_isRunning || environment != PublisherEnvironment.Local)
        {
            return;
        }

        _selectedEnvironment = environment;
    }

    private string GetEnvironmentCardClass(PublisherEnvironment environment)
    {
        var classes = "environment-card";

        if (_selectedEnvironment == environment)
        {
            classes += " environment-card-selected";
        }

        if (environment != PublisherEnvironment.Local)
        {
            classes += " environment-card-disabled";
        }
        else
        {
            classes += " environment-card-selectable";
        }

        return classes;
    }

    private async Task RunLocalBuildAsync()
    {
        if (_isRunning || _selectedEnvironment != PublisherEnvironment.Local)
        {
            return;
        }

        ResetState();
        var runCts = new CancellationTokenSource();
        _runCts = runCts;
        _isRunning = true;

        try
        {
            await PublishService.RunAsync(ReportProgressAsync, runCts.Token);
            _completed = true;
        }
        catch (OperationCanceledException)
        {
            _blockingError = "The local build was stopped.";
        }
        catch (PublisherException exception)
        {
            _blockingError = exception.Message;
        }
        finally
        {
            _isRunning = false;
            if (ReferenceEquals(_runCts, runCts))
            {
                _runCts = null;
            }

            runCts.Dispose();
        }
    }

    private Task ReportProgressAsync(PublishProgress progress)
    {
        return InvokeAsync(() =>
        {
            var step = _steps.Single(item => item.Id == progress.Step);
            step.Status = progress.Status;
            step.ErrorMessage = progress.ErrorMessage;
            StateHasChanged();
        });
    }

    private void CancelBuild()
    {
        _runCts?.Cancel();
    }

    private void ResetState()
    {
        _completed = false;
        _blockingError = null;

        foreach (var step in _steps)
        {
            step.Status = PublishStepStatus.Pending;
            step.ErrorMessage = null;
        }
    }

    private static string GetStepIcon(PublishStepId stepId)
    {
        return stepId switch
        {
            PublishStepId.Prerequisites => Icons.Material.Filled.Rule,
            PublishStepId.Docker => Icons.Material.Filled.Dns,
            PublishStepId.Web => Icons.Material.Filled.Language,
            PublishStepId.Desktop => Icons.Material.Filled.LaptopChromebook,
            PublishStepId.Mobile => Icons.Material.Filled.Android,
            _ => Icons.Material.Filled.RadioButtonUnchecked
        };
    }

    private static MudBlazor.Color GetStepColor(PublishStepStatus status)
    {
        return status switch
        {
            PublishStepStatus.Running => MudBlazor.Color.Info,
            PublishStepStatus.Succeeded => MudBlazor.Color.Success,
            PublishStepStatus.Failed => MudBlazor.Color.Error,
            _ => MudBlazor.Color.Default
        };
    }

    private static string GetStepStatusText(PublishStepStatus status)
    {
        return status switch
        {
            PublishStepStatus.Running => "In progress",
            PublishStepStatus.Succeeded => "Completed",
            PublishStepStatus.Failed => "Failed",
            _ => "Waiting"
        };
    }

    public ValueTask DisposeAsync()
    {
        _runCts?.Cancel();
        return ValueTask.CompletedTask;
    }
}
