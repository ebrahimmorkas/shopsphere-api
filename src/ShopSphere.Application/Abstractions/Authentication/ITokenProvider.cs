using ShopSphere.Domain.Users;

namespace ShopSphere.Application.Abstractions.Authentication;

public interface ITokenProvider
{
    AccessToken CreateAccessToken(User user);

    string CreateRefreshToken();

    TimeSpan RefreshTokenLifetime { get; }
}

public sealed record AccessToken(string Token, DateTime ExpiresAtUtc);
