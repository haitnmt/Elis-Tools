using System.Security.Claims;
using System.Text;
using Haihv.Elis.Tool.TraCuuGcn.Models;
using Haihv.Elis.Tool.TraCuuGcn.Web_Api.Data;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Haihv.Elis.Tool.TraCuuGcn.Web_Api.Authenticate;

public sealed class TokenProvider(
    string secretKey = "Jwt:SecretKey",
    string issuer = "Jwt:Issuer",
    string audience = "Jwt:Audience",
    int expiryMinutes = 60)
{
    public AccessToken GenerateToken(AuthChuSuDung chuSuDung)
    {
        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);
        var tokenHandler = new JsonWebTokenHandler();
        var id = Guid.CreateVersion7().ToString();
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Jti, id),
            new("SoDinhDanh", chuSuDung.SoDinhDanh),
            new("HoVaTen", chuSuDung.HoVaTen)
        };

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddMinutes(expiryMinutes),
            Issuer = issuer,
            Audience = audience,
            SigningCredentials = credentials
        };

        return new AccessToken(tokenHandler.CreateToken(tokenDescriptor), id);
    }
}

public static class TokenExtensions
{
    public static string? GetSoDinhDanh(this ClaimsPrincipal claimsPrincipal) =>
        claimsPrincipal.FindFirst("SoDinhDanh")?.Value;

    public static string? GetHoVaTen(this ClaimsPrincipal claimsPrincipal) =>
        claimsPrincipal.FindFirst("HoVaTen")?.Value;
}