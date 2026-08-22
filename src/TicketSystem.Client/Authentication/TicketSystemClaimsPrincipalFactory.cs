using System.Security.Claims;
using TicketSystem.Shared.Authentication;
using TicketSystem.Shared.Enums;

namespace TicketSystem.Client.Authentication;

public static class TicketSystemClaimsPrincipalFactory
{
    public static ClaimsPrincipal Create(LoginResponse loginResponse, string authenticationType)
    {
        var role = ((AppUserType)loginResponse.UserTypeId).ToString();
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, loginResponse.UserId.ToString()),
            new Claim(ClaimTypes.Name, $"{loginResponse.FirstName} {loginResponse.LastName}"),
            new Claim(ClaimTypes.GivenName, loginResponse.FirstName),
            new Claim(ClaimTypes.Surname, loginResponse.LastName),
            new Claim(ClaimTypes.Email, loginResponse.Email),
            new Claim(ClaimTypes.Role, role),
            new Claim(TicketSystemClaimTypes.ApiAccessToken, loginResponse.AccessToken)
        };
        return new ClaimsPrincipal(new ClaimsIdentity(claims, authenticationType));
    }
}
