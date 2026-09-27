using ShopSphere.Domain.Abstractions;

namespace ShopSphere.Domain.Users;

/// <summary>
/// Opaque refresh token. Tokens are rotated on every use; reusing a revoked token is treated as theft
/// and revokes every active token of the user.
/// </summary>
public sealed class RefreshToken : Entity
{
    private RefreshToken(Guid id, string token, Guid userId, DateTime expiresAtUtc, DateTime createdAtUtc)
        : base(id)
    {
        Token = token;
        UserId = userId;
        ExpiresAtUtc = expiresAtUtc;
        CreatedAtUtc = createdAtUtc;
    }

    private RefreshToken()
    {
    }

    public string Token { get; private set; } = string.Empty;

    public Guid UserId { get; private set; }

    public DateTime ExpiresAtUtc { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public DateTime? RevokedAtUtc { get; private set; }

    public bool IsActive(DateTime utcNow) => RevokedAtUtc is null && ExpiresAtUtc > utcNow;

    public static RefreshToken Create(string token, Guid userId, DateTime expiresAtUtc, DateTime utcNow) =>
        new(Guid.CreateVersion7(), token, userId, expiresAtUtc, utcNow);

    public void Revoke(DateTime utcNow) => RevokedAtUtc ??= utcNow;
}
