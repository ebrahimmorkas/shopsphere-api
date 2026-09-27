using ShopSphere.Application.Abstractions.Authentication;
using ShopSphere.Application.Abstractions.Data;
using ShopSphere.Domain.Users;

namespace ShopSphere.Application.Users;

public sealed record TokenResponse(string AccessToken, DateTime AccessTokenExpiresAtUtc, string RefreshToken);

internal static class TokenIssuer
{
    public static TokenResponse Issue(
        User user,
        ITokenProvider tokenProvider,
        IApplicationDbContext dbContext,
        DateTime utcNow)
    {
        var accessToken = tokenProvider.CreateAccessToken(user);
        var refreshToken = RefreshToken.Create(
            tokenProvider.CreateRefreshToken(),
            user.Id,
            utcNow.Add(tokenProvider.RefreshTokenLifetime),
            utcNow);

        dbContext.RefreshTokens.Add(refreshToken);

        return new TokenResponse(accessToken.Token, accessToken.ExpiresAtUtc, refreshToken.Token);
    }
}
