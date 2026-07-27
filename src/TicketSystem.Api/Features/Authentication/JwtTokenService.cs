using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using TicketSystem.DAL.AppUsers;
using TicketSystem.Shared.Enums;

namespace TicketSystem.Api.Features.Authentication;

public sealed class JwtTokenService
{
    private static readonly TimeSpan TokenLifetime = TimeSpan.FromHours(8);

    private readonly JwtOptions jwtOptions;

    public JwtTokenService(JwtOptions jwtOptions)
    {
        this.jwtOptions = jwtOptions;
    }

    public LoginResponse Create(AppUserLoginData appUser)
    {
        var expiresAt = DateTimeOffset.UtcNow.Add(TokenLifetime);
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, appUser.Id.ToString()),
            new Claim(ClaimTypes.Name, $"{appUser.FirstName} {appUser.LastName}"),
            new Claim(ClaimTypes.GivenName, appUser.FirstName),
            new Claim(ClaimTypes.Surname, appUser.LastName),
            new Claim(ClaimTypes.Email, appUser.Email),
            new Claim(ClaimTypes.Role, ((AppUserType)appUser.UserTypeId).ToString())
        };
        var signingCredentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SigningKey)),
            SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: jwtOptions.Issuer,
            audience: jwtOptions.Audience,
            claims: claims,
            expires: expiresAt.UtcDateTime,
            signingCredentials: signingCredentials);
        var accessToken = new JwtSecurityTokenHandler().WriteToken(token);

        return new LoginResponse(accessToken, expiresAt, appUser.Id, appUser.Email, appUser.FirstName, appUser.LastName, appUser.UserTypeId);
    }
}
