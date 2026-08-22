namespace TicketSystem.Publisher.Services;

public sealed class PublisherException : Exception
{
    public PublisherException(string message) : base(message)
    {
    }

    public PublisherException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
