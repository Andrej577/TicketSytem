namespace TicketSystem.DAL.Chat;

public sealed record ChatCollectionResult<T>(bool TicketFound, IReadOnlyList<T> Items);

public sealed record ChatWriteResult<T>(bool TicketFound, bool TicketClosed, bool TicketChanged, Guid CustomerId, T? Item)
{
    public static ChatWriteResult<T> NotFound() => new(false, false, false, Guid.Empty, default);

    public static ChatWriteResult<T> Closed() => new(true, true, false, Guid.Empty, default);

    public static ChatWriteResult<T> Success(T item, bool ticketChanged, Guid customerId) => new(true, false, ticketChanged, customerId, item);
}

public sealed class MediaFileDownload
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Extension { get; set; } = string.Empty;

    public string ContentType { get; set; } = string.Empty;

    public byte[] Content { get; set; } = [];
}
