using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using ShopSphere.Application.Abstractions.Authentication;
using ShopSphere.Domain.Users;

namespace ShopSphere.Infrastructure.Authentication;

internal sealed class TokenProvider(IOptions<JwtOptions> options, TimeProvider timeProvider) : ITokenProvider
{
    private readonly JwtOptions _options = options.Value;

    public TimeSpan RefreshTokenLifetime => TimeSpan.FromDays(_options.RefreshTokenLifetimeDays);

    public AccessToken CreateAccessToken(User user)
    {
        var expiresAtUtc = timeProvider.GetUtcNow().UtcDateTime.AddMinutes(_options.AccessTokenLifetimeMinutes);
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.Secret));

        var descriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(
            [
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
                new Claim(JwtRegisteredClaimNames.Name, user.FullName),
                new Claim(CustomClaims.Role, user.Role.ToString())
            ]),
            Expires = expiresAtUtc,
            Issuer = _options.Issuer,
            Audience = _options.Audience,
            SigningCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256)
        };

        return new AccessToken(new JsonWebTokenHandler().CreateToken(descriptor), expiresAtUtc);
    }

    public string CreateRefreshToken() =>
        Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
}
