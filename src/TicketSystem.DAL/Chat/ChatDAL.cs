using Dapper;
using Npgsql;
using TicketSystem.Shared.POCO;

namespace TicketSystem.DAL.Chat;

public sealed class ChatDAL
{
    private const string AppUserTable = "AppUser";
    private const string ChatSessionTable = "ChatSession";
    private const string MediaFileTable = "MediaFile";
    private const string MessageTable = "Message";
    private const string TicketTable = "Ticket";
    private const short ClosedTicketStatusId = 4;

    private readonly NpgsqlDataSource dataSource;

    public ChatDAL(NpgsqlDataSource dataSource)
    {
        this.dataSource = dataSource;
    }

    public async Task<ChatCollectionResult<MessagePOCO>> GetMessagesAsync(Guid ticketId, Guid currentUserId, bool customerOnly, CancellationToken cancellationToken)
    {
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        if (!await TicketExistsAsync(connection, ticketId, currentUserId, customerOnly, cancellationToken))
        {
            return new ChatCollectionResult<MessagePOCO>(false, []);
        }

        var sql = $"""
            SELECT
                "Message".*,
                "Sender"."FirstName" AS "SenderFirstName",
                "Sender"."LastName" AS "SenderLastName"
            FROM "{TicketTable}" AS "Ticket"
            INNER JOIN "{MessageTable}" AS "Message" ON "Message"."ChatSessionId" = "Ticket"."ChatSessionId"
            INNER JOIN "{AppUserTable}" AS "Sender" ON "Sender"."Id" = "Message"."SenderId"
            WHERE "Ticket"."Id" = @TicketId
                AND "Ticket"."IsDeleted" = false
                AND (NOT @CustomerOnly OR "Ticket"."CustomerId" = @CurrentUserId)
            ORDER BY "Message"."SentAt";
            """;
        var command = new CommandDefinition(sql, new { TicketId = ticketId, CurrentUserId = currentUserId, CustomerOnly = customerOnly }, cancellationToken: cancellationToken);
        return new ChatCollectionResult<MessagePOCO>(true, (await connection.QueryAsync<MessagePOCO>(command)).ToList());
    }

    public async Task<ChatCollectionResult<MediaFilePOCO>> GetMediaFilesAsync(Guid ticketId, Guid currentUserId, bool customerOnly, CancellationToken cancellationToken)
    {
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        if (!await TicketExistsAsync(connection, ticketId, currentUserId, customerOnly, cancellationToken))
        {
            return new ChatCollectionResult<MediaFilePOCO>(false, []);
        }

        var sql = $"""
            SELECT
                "MediaFile"."Id",
                "MediaFile"."ChatSessionId",
                "MediaFile"."UploadedByUserId",
                "MediaFile"."Name",
                "MediaFile"."Extension",
                "MediaFile"."ContentType",
                "MediaFile"."SizeBytes",
                "MediaFile"."CreatedAt",
                "Uploader"."FirstName" AS "UploaderFirstName",
                "Uploader"."LastName" AS "UploaderLastName"
            FROM "{TicketTable}" AS "Ticket"
            INNER JOIN "{MediaFileTable}" AS "MediaFile" ON "MediaFile"."ChatSessionId" = "Ticket"."ChatSessionId"
            INNER JOIN "{AppUserTable}" AS "Uploader" ON "Uploader"."Id" = "MediaFile"."UploadedByUserId"
            WHERE "Ticket"."Id" = @TicketId
                AND "Ticket"."IsDeleted" = false
                AND (NOT @CustomerOnly OR "Ticket"."CustomerId" = @CurrentUserId)
            ORDER BY "MediaFile"."CreatedAt" DESC;
            """;
        var command = new CommandDefinition(sql, new { TicketId = ticketId, CurrentUserId = currentUserId, CustomerOnly = customerOnly }, cancellationToken: cancellationToken);
        return new ChatCollectionResult<MediaFilePOCO>(true, (await connection.QueryAsync<MediaFilePOCO>(command)).ToList());
    }

    public async Task<ChatWriteResult<MessagePOCO>> CreateMessageAsync(Guid ticketId, Guid senderId, bool customerOnly, bool assignToCurrentUser, string content, CancellationToken cancellationToken)
    {
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        var preparation = await PrepareChatAsync(connection, transaction, ticketId, senderId, customerOnly, assignToCurrentUser, cancellationToken);
        if (!preparation.TicketFound)
        {
            return ChatWriteResult<MessagePOCO>.NotFound();
        }

        if (preparation.TicketClosed)
        {
            return ChatWriteResult<MessagePOCO>.Closed();
        }

        var sql = $"""
            WITH "CreatedMessage" AS (
                INSERT INTO "{MessageTable}" ("ChatSessionId", "TicketId", "SenderId", "Content")
                VALUES (@ChatSessionId, @TicketId, @SenderId, @Content)
                RETURNING *
            )
            SELECT
                "CreatedMessage".*,
                "Sender"."FirstName" AS "SenderFirstName",
                "Sender"."LastName" AS "SenderLastName"
            FROM "CreatedMessage"
            INNER JOIN "{AppUserTable}" AS "Sender" ON "Sender"."Id" = "CreatedMessage"."SenderId";
            """;
        var parameters = new { ChatSessionId = preparation.ChatSessionId, TicketId = ticketId, SenderId = senderId, Content = content };
        var command = new CommandDefinition(sql, parameters, transaction, cancellationToken: cancellationToken);
        var message = await connection.QuerySingleAsync<MessagePOCO>(command);
        await transaction.CommitAsync(cancellationToken);
        return ChatWriteResult<MessagePOCO>.Success(message, preparation.TicketChanged, preparation.CustomerId);
    }

    public async Task<ChatWriteResult<MediaFilePOCO>> CreateMediaFileAsync(Guid ticketId, Guid uploadedByUserId, bool customerOnly, bool assignToCurrentUser, string name, string extension, string contentType, byte[] content, CancellationToken cancellationToken)
    {
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        var preparation = await PrepareChatAsync(connection, transaction, ticketId, uploadedByUserId, customerOnly, assignToCurrentUser, cancellationToken);
        if (!preparation.TicketFound)
        {
            return ChatWriteResult<MediaFilePOCO>.NotFound();
        }

        if (preparation.TicketClosed)
        {
            return ChatWriteResult<MediaFilePOCO>.Closed();
        }

        var sql = $"""
            WITH "CreatedMediaFile" AS (
                INSERT INTO "{MediaFileTable}" (
                    "ChatSessionId",
                    "UploadedByUserId",
                    "Name",
                    "Extension",
                    "ContentType",
                    "SizeBytes",
                    "Content"
                )
                VALUES (
                    @ChatSessionId,
                    @UploadedByUserId,
                    @Name,
                    @Extension,
                    @ContentType,
                    @SizeBytes,
                    @Content
                )
                RETURNING
                    "Id",
                    "ChatSessionId",
                    "UploadedByUserId",
                    "Name",
                    "Extension",
                    "ContentType",
                    "SizeBytes",
                    "CreatedAt"
            )
            SELECT
                "CreatedMediaFile".*,
                "Uploader"."FirstName" AS "UploaderFirstName",
                "Uploader"."LastName" AS "UploaderLastName"
            FROM "CreatedMediaFile"
            INNER JOIN "{AppUserTable}" AS "Uploader" ON "Uploader"."Id" = "CreatedMediaFile"."UploadedByUserId";
            """;
        var parameters = new { ChatSessionId = preparation.ChatSessionId, UploadedByUserId = uploadedByUserId, Name = name, Extension = extension, ContentType = contentType, SizeBytes = content.LongLength, Content = content };
        var command = new CommandDefinition(sql, parameters, transaction, cancellationToken: cancellationToken);
        var mediaFile = await connection.QuerySingleAsync<MediaFilePOCO>(command);
        await transaction.CommitAsync(cancellationToken);
        return ChatWriteResult<MediaFilePOCO>.Success(mediaFile, preparation.TicketChanged, preparation.CustomerId);
    }

    public async Task<MediaFileDownload?> GetMediaFileAsync(Guid ticketId, Guid mediaFileId, Guid currentUserId, bool customerOnly, CancellationToken cancellationToken)
    {
        var sql = $"""
            SELECT
                "MediaFile"."Id",
                "MediaFile"."Name",
                "MediaFile"."Extension",
                "MediaFile"."ContentType",
                "MediaFile"."Content"
            FROM "{TicketTable}" AS "Ticket"
            INNER JOIN "{MediaFileTable}" AS "MediaFile" ON "MediaFile"."ChatSessionId" = "Ticket"."ChatSessionId"
            WHERE "Ticket"."Id" = @TicketId
                AND "MediaFile"."Id" = @MediaFileId
                AND "Ticket"."IsDeleted" = false
                AND (NOT @CustomerOnly OR "Ticket"."CustomerId" = @CurrentUserId);
            """;

        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        var parameters = new { TicketId = ticketId, MediaFileId = mediaFileId, CurrentUserId = currentUserId, CustomerOnly = customerOnly };
        var command = new CommandDefinition(sql, parameters, cancellationToken: cancellationToken);
        return await connection.QuerySingleOrDefaultAsync<MediaFileDownload>(command);
    }

    private static async Task<ChatPreparationResult> PrepareChatAsync(NpgsqlConnection connection, NpgsqlTransaction transaction, Guid ticketId, Guid currentUserId, bool customerOnly, bool assignToCurrentUser, CancellationToken cancellationToken)
    {
        var ticketSql = $"""
            SELECT "Id", "ChatSessionId", "CustomerId", "OperatorId", "Title", "StatusId"
            FROM "{TicketTable}"
            WHERE "Id" = @TicketId
                AND "IsDeleted" = false
                AND (NOT @CustomerOnly OR "CustomerId" = @CurrentUserId)
            FOR UPDATE;
            """;
        var ticketCommand = new CommandDefinition(ticketSql, new { TicketId = ticketId, CurrentUserId = currentUserId, CustomerOnly = customerOnly }, transaction, cancellationToken: cancellationToken);
        var ticket = await connection.QuerySingleOrDefaultAsync<ChatTicketState>(ticketCommand);
        if (ticket is null)
        {
            return ChatPreparationResult.NotFound();
        }

        if (ticket.StatusId == ClosedTicketStatusId)
        {
            return ChatPreparationResult.Closed(ticket.CustomerId);
        }

        var operatorId = assignToCurrentUser ? currentUserId : ticket.OperatorId;
        var ticketChanged = false;
        var chatSessionId = ticket.ChatSessionId;
        if (!chatSessionId.HasValue)
        {
            var createSessionSql = $"""
                INSERT INTO "{ChatSessionTable}" ("CustomerId", "OperatorId", "Title", "StatusId")
                VALUES (@CustomerId, @OperatorId, @Title, 1)
                RETURNING "Id";
                """;
            var createSessionParameters = new { ticket.CustomerId, OperatorId = operatorId, ticket.Title };
            var createSessionCommand = new CommandDefinition(createSessionSql, createSessionParameters, transaction, cancellationToken: cancellationToken);
            chatSessionId = await connection.QuerySingleAsync<Guid>(createSessionCommand);

            var updateTicketSql = $"""UPDATE "{TicketTable}" SET "ChatSessionId" = @ChatSessionId, "OperatorId" = @OperatorId, "UpdatedAt" = now(), "UpdatedByUserId" = @CurrentUserId WHERE "Id" = @TicketId;""";
            var updateTicketParameters = new { ChatSessionId = chatSessionId.Value, OperatorId = operatorId, CurrentUserId = currentUserId, TicketId = ticketId };
            var updateTicketCommand = new CommandDefinition(updateTicketSql, updateTicketParameters, transaction, cancellationToken: cancellationToken);
            await connection.ExecuteAsync(updateTicketCommand);
            ticketChanged = true;
        }
        else if (assignToCurrentUser && ticket.OperatorId != currentUserId)
        {
            var assignTicketSql = $"""UPDATE "{TicketTable}" SET "OperatorId" = @CurrentUserId, "UpdatedAt" = now(), "UpdatedByUserId" = @CurrentUserId WHERE "Id" = @TicketId;""";
            var assignTicketCommand = new CommandDefinition(assignTicketSql, new { CurrentUserId = currentUserId, TicketId = ticketId }, transaction, cancellationToken: cancellationToken);
            await connection.ExecuteAsync(assignTicketCommand);

            var assignSessionSql = $"""UPDATE "{ChatSessionTable}" SET "OperatorId" = @CurrentUserId WHERE "Id" = @ChatSessionId;""";
            var assignSessionCommand = new CommandDefinition(assignSessionSql, new { CurrentUserId = currentUserId, ChatSessionId = chatSessionId.Value }, transaction, cancellationToken: cancellationToken);
            await connection.ExecuteAsync(assignSessionCommand);
            ticketChanged = true;
        }

        return ChatPreparationResult.Success(chatSessionId.Value, ticket.CustomerId, ticketChanged);
    }

    private static async Task<bool> TicketExistsAsync(NpgsqlConnection connection, Guid ticketId, Guid currentUserId, bool customerOnly, CancellationToken cancellationToken)
    {
        var sql = $"""SELECT EXISTS (SELECT 1 FROM "{TicketTable}" WHERE "Id" = @TicketId AND "IsDeleted" = false AND (NOT @CustomerOnly OR "CustomerId" = @CurrentUserId));""";
        var command = new CommandDefinition(sql, new { TicketId = ticketId, CurrentUserId = currentUserId, CustomerOnly = customerOnly }, cancellationToken: cancellationToken);
        return await connection.QuerySingleAsync<bool>(command);
    }

    private sealed class ChatTicketState
    {
        public Guid Id { get; set; }

        public Guid? ChatSessionId { get; set; }

        public Guid CustomerId { get; set; }

        public Guid? OperatorId { get; set; }

        public string Title { get; set; } = string.Empty;

        public short StatusId { get; set; }
    }

    private sealed record ChatPreparationResult(bool TicketFound, bool TicketClosed, Guid? ChatSessionId, Guid CustomerId, bool TicketChanged)
    {
        public static ChatPreparationResult NotFound() => new(false, false, null, Guid.Empty, false);

        public static ChatPreparationResult Closed(Guid customerId) => new(true, true, null, customerId, false);

        public static ChatPreparationResult Success(Guid chatSessionId, Guid customerId, bool ticketChanged) => new(true, false, chatSessionId, customerId, ticketChanged);
    }
}
