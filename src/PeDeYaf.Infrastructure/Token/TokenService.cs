using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using PeDeYaf.Application.Common.Interfaces;
using PeDeYaf.Domain.Entities;

namespace PeDeYaf.Infrastructure.Token;

public class TokenService(IConfiguration configuration) : ITokenService
{
    public Task<(string AccessToken, string RefreshToken)> GenerateTokenPairAsync(
        User user, CancellationToken ct = default)
    {
        var secret = configuration["Jwt:Secret"] ?? "superSecretKeyThatIsAtLeast32BytesLong!";
        var issuer = configuration["Jwt:Issuer"] ?? "PeDeYaf";
        var audience = configuration["Jwt:Audience"] ?? "PeDeYafUsers";

        var key = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim("phone", user.Phone),
            new Claim("plan", user.Plan.ToString()),
        };

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(15),
            signingCredentials: creds
        );

        var accessToken = new JwtSecurityTokenHandler().WriteToken(token);

        // Refresh token: opaque random string, NOT a JWT
        var refreshBytes = new byte[64];
        System.Security.Cryptography.RandomNumberGenerator.Fill(refreshBytes);
        var refreshToken = Convert.ToBase64String(refreshBytes);

        return Task.FromResult((accessToken, refreshToken));
    }
}
