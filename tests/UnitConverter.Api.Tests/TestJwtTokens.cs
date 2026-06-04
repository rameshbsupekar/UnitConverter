using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace UnitConverter.UnitsDefinitions.Api.Tests;

internal static class TestJwtTokens
{
    private const string Secret = "dev-only-change-in-production-min-32-chars!!";
    private const string Issuer = "https://localhost:7180";
    private const string Audience = "unitconverter-api";

    public static string CreateForRole(string role, string email = "tester@unitconverter.local") =>
        Create([role], email);

    public static string Create(string[] roles, string email = "tester@unitconverter.local")
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, "1"),
            new(ClaimTypes.Email, email),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Secret));
        var token = new JwtSecurityToken(
            issuer: Issuer,
            audience: Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
