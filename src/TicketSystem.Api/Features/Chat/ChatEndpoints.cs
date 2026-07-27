using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using TicketSystem.Api.Features.Tickets;
using TicketSystem.DAL.Chat;
using TicketSystem.Shared.Enums;
using TicketSystem.Shared.POCO;

namespace TicketSystem.Api.Features.Chat;

public static class ChatEndpoints
{
    private const long MaximumFileSize = 10 * 1024 * 1024;
    private const long MaximumRequestSize = MaximumFileSize + 64 * 1024;
    private static readonly byte[] PngSignature = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];
    private static readonly byte[] JpegSignature = [0xFF, 0xD8, 0xFF];
    private static readonly byte[] LegacyOfficeSignature = [0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1];
    private static readonly byte[] OpenXmlSignature = [0x50, 0x4B, 0x03, 0x04];

    private static readonly IReadOnlyDictionary<string, string> AllowedFileTypes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        [".pdf"] = "application/pdf",
        [".doc"] = "application/msword",
        [".docx"] = "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        [".xls"] = "application/vnd.ms-excel",
        [".xlsx"] = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        [".png"] = "image/png",
        [".jpg"] = "image/jpeg",
        [".jpeg"] = "image/jpeg"
    };

    public static IEndpointRouteBuilder MapChatEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/tickets/{ticketId:guid}/chat")
            .WithTags("Chat")
            .RequireAuthorization(policy => policy.RequireRole(nameof(AppUserType.Customer), nameof(AppUserType.Operator), nameof(AppUserType.Administrator)));

        group.MapGet("/messages", GetMessages).WithName("GetChatMessages").Produces<IReadOnlyList<MessagePOCO>>(StatusCodes.Status200OK).Produces(StatusCodes.Status404NotFound);
        group.MapPost("/messages", SendMessage).WithName("SendChatMessage").Produces<MessagePOCO>(StatusCodes.Status201Created).Produces(StatusCodes.Status404NotFound).Produces(StatusCodes.Status409Conflict);
        group.MapGet("/media", GetMediaFiles).WithName("GetChatMediaFiles").Produces<IReadOnlyList<MediaFilePOCO>>(StatusCodes.Status200OK).Produces(StatusCodes.Status404NotFound);
        group.MapPost("/media", UploadMediaFile).DisableAntiforgery().WithMetadata(new RequestSizeLimitAttribute(MaximumRequestSize)).WithName("UploadChatMediaFile").Produces<MediaFilePOCO>(StatusCodes.Status201Created).Produces(StatusCodes.Status400BadRequest).Produces(StatusCodes.Status404NotFound).Produces(StatusCodes.Status409Conflict).Produces(StatusCodes.Status413PayloadTooLarge);
        group.MapGet("/media/{mediaFileId:guid}", DownloadMediaFile).WithName("DownloadChatMediaFile").Produces(StatusCodes.Status200OK).Produces(StatusCodes.Status404NotFound);

        return endpoints;
    }

    private static async Task<IResult> GetMessages(Guid ticketId, ClaimsPrincipal user, ChatDAL chatDAL, CancellationToken cancellationToken)
    {
        var result = await chatDAL.GetMessagesAsync(ticketId, GetCurrentUserId(user), IsCustomer(user), cancellationToken);
        return result.TicketFound ? Results.Ok(result.Items) : Results.NotFound();
    }

    private static async Task<IResult> SendMessage(Guid ticketId, SendMessageRequest request, ClaimsPrincipal user, ChatDAL chatDAL, ChatRealtimeNotifier chatRealtimeNotifier, TicketRealtimeNotifier ticketRealtimeNotifier, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Content))
        {
            return Results.ValidationProblem(new Dictionary<string, string[]> { [nameof(request.Content)] = ["Message content is required."] });
        }

        var currentUserId = GetCurrentUserId(user);
        var result = await chatDAL.CreateMessageAsync(ticketId, currentUserId, IsCustomer(user), IsOperator(user) && request.AssignToCurrentUser, request.Content.Trim(), cancellationToken);
        if (!result.TicketFound)
        {
            return Results.NotFound();
        }

        if (result.TicketClosed)
        {
            return Results.Conflict(new { message = "Closed tickets do not accept new messages." });
        }

        var message = result.Item!;
        if (result.TicketChanged)
        {
            await ticketRealtimeNotifier.NotifyUpdatedTicketAsync(ticketId);
        }

        await chatRealtimeNotifier.NotifyChangedAsync(ticketId, message.ChatSessionId, result.CustomerId);
        return Results.Created($"/api/tickets/{ticketId}/chat/messages/{message.Id}", message);
    }

    private static async Task<IResult> GetMediaFiles(Guid ticketId, ClaimsPrincipal user, ChatDAL chatDAL, CancellationToken cancellationToken)
    {
        var result = await chatDAL.GetMediaFilesAsync(ticketId, GetCurrentUserId(user), IsCustomer(user), cancellationToken);
        return result.TicketFound ? Results.Ok(result.Items) : Results.NotFound();
    }

    private static async Task<IResult> UploadMediaFile(Guid ticketId, HttpRequest request, ClaimsPrincipal user, ChatDAL chatDAL, ChatRealtimeNotifier chatRealtimeNotifier, TicketRealtimeNotifier ticketRealtimeNotifier, CancellationToken cancellationToken)
    {
        if (!request.HasFormContentType)
        {
            return Results.BadRequest(new { message = "A multipart form with a file is required." });
        }

        var form = await request.ReadFormAsync(cancellationToken);
        var file = form.Files.GetFile("File");
        if (file is null)
        {
            return Results.BadRequest(new { message = "A file is required." });
        }

        if (file.Length > MaximumFileSize)
        {
            return Results.StatusCode(StatusCodes.Status413PayloadTooLarge);
        }

        var fileName = Path.GetFileName(file.FileName.Replace('\\', '/'));
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        if (!AllowedFileTypes.TryGetValue(extension, out var contentType))
        {
            return Results.BadRequest(new { message = "Allowed file types are PDF, Word, Excel, PNG and JPG." });
        }

        var name = Path.GetFileNameWithoutExtension(fileName);
        if (string.IsNullOrWhiteSpace(name) || name.Length > 255)
        {
            return Results.BadRequest(new { message = "The file name must contain between 1 and 255 characters without the extension." });
        }

        await using var memoryStream = new MemoryStream((int)file.Length);
        await file.CopyToAsync(memoryStream, cancellationToken);
        var content = memoryStream.ToArray();
        if (!HasExpectedFileSignature(extension, content))
        {
            return Results.BadRequest(new { message = "The file content does not match its extension." });
        }

        var currentUserId = GetCurrentUserId(user);
        var assignToCurrentUser = IsOperator(user) && bool.TryParse(form["AssignToCurrentUser"], out var assign) && assign;
        var result = await chatDAL.CreateMediaFileAsync(ticketId, currentUserId, IsCustomer(user), assignToCurrentUser, name, extension, contentType, content, cancellationToken);
        if (!result.TicketFound)
        {
            return Results.NotFound();
        }

        if (result.TicketClosed)
        {
            return Results.Conflict(new { message = "Closed tickets do not accept new files." });
        }

        var mediaFile = result.Item!;
        if (result.TicketChanged)
        {
            await ticketRealtimeNotifier.NotifyUpdatedTicketAsync(ticketId);
        }

        await chatRealtimeNotifier.NotifyChangedAsync(ticketId, mediaFile.ChatSessionId, result.CustomerId);
        return Results.Created($"/api/tickets/{ticketId}/chat/media/{mediaFile.Id}", mediaFile);
    }

    private static async Task<IResult> DownloadMediaFile(Guid ticketId, Guid mediaFileId, ClaimsPrincipal user, ChatDAL chatDAL, CancellationToken cancellationToken)
    {
        var mediaFile = await chatDAL.GetMediaFileAsync(ticketId, mediaFileId, GetCurrentUserId(user), IsCustomer(user), cancellationToken);
        return mediaFile is null
            ? Results.NotFound()
            : Results.File(mediaFile.Content, mediaFile.ContentType, $"{mediaFile.Name}{mediaFile.Extension}");
    }

    private static Guid GetCurrentUserId(ClaimsPrincipal user)
    {
        return Guid.Parse(user.FindFirst(ClaimTypes.NameIdentifier)!.Value);
    }

    private static bool IsCustomer(ClaimsPrincipal user)
    {
        return user.IsInRole(nameof(AppUserType.Customer));
    }

    private static bool IsOperator(ClaimsPrincipal user)
    {
        return user.IsInRole(nameof(AppUserType.Operator));
    }

    private static bool HasExpectedFileSignature(string extension, ReadOnlySpan<byte> content)
    {
        return extension switch
        {
            ".pdf" => content.StartsWith("%PDF-"u8),
            ".png" => content.StartsWith(PngSignature),
            ".jpg" or ".jpeg" => content.StartsWith(JpegSignature),
            ".doc" or ".xls" => content.StartsWith(LegacyOfficeSignature),
            ".docx" or ".xlsx" => content.StartsWith(OpenXmlSignature),
            _ => false
        };
    }
}
