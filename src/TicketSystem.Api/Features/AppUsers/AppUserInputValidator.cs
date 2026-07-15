using System.Net.Mail;

namespace TicketSystem.Api.Features.AppUsers;

internal static class AppUserInputValidator
{
    public static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();
}
