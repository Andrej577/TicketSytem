namespace TicketSystem.Publisher.Models;

public enum PublisherEnvironment
{
    Local,
    Test,
    Production
}

public enum PublishStepId
{
    Prerequisites,
    Docker,
    Web,
    Desktop,
    Mobile
}

public enum PublishStepStatus
{
    Pending,
    Running,
    Succeeded,
    Failed
}

public sealed record PublishProgress(PublishStepId Step, PublishStepStatus Status, string? ErrorMessage = null);

public sealed class PublishStepViewModel(PublishStepId id, string title, string description)
{
    public PublishStepId Id { get; } = id;
    public string Title { get; } = title;
    public string Description { get; } = description;
    public PublishStepStatus Status { get; set; } = PublishStepStatus.Pending;
    public string? ErrorMessage { get; set; }
}
