using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using AuthService.Models;
using Microsoft.IdentityModel.Tokens;

namespace AuthService.Services;

public record TokenPair(string AccessToken, string RefreshToken, DateTime AccessExpiresAt, DateTime RefreshExpiresAt);

public class JwtService
{
    private readonly IConfiguration _config;
    public JwtService(IConfiguration config) => _config = config;

    public TokenPair IssueTokens(User user, IReadOnlyList<string> roles, string refreshToken)
    {
        var jwt = _config.GetSection("Jwt");
        var issuer = jwt["Issuer"]!;
        var audience = jwt["Audience"]!;
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt["Key"]!));
        var accessMinutes = int.Parse(jwt["AccessTokenMinutes"] ?? "15");
        var refreshDays = int.Parse(jwt["RefreshTokenDays"] ?? "14");

        var now = DateTime.UtcNow;
        var accessExpires = now.AddMinutes(accessMinutes);
        var refreshExpires = now.AddDays(refreshDays);

        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.UniqueName, user.Username),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim("is_active", user.IsActive.ToString())
        };
        claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));

        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            notBefore: now,
            expires: accessExpires,
            signingCredentials: creds);

        var accessToken = new JwtSecurityTokenHandler().WriteToken(token);

        return new TokenPair(accessToken, refreshToken, accessExpires, refreshExpires);
    }
}