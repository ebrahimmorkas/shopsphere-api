using ShopSphere.Domain.Abstractions;

namespace ShopSphere.Domain.Users;

public static class UserErrors
{
    public static readonly Error EmailNotUnique =
        Error.Conflict("User.EmailNotUnique", "The provided email is already registered.");

    // Deliberately generic so attackers can't enumerate registered emails.
    public static readonly Error InvalidCredentials =
        Error.Unauthorized("User.InvalidCredentials", "The email or password is incorrect.");

    public static readonly Error InvalidRefreshToken =
        Error.Unauthorized("User.InvalidRefreshToken", "The refresh token is invalid or has expired.");

    public static Error NotFound(Guid id) =>
        Error.NotFound("User.NotFound", $"The user with id '{id}' was not found.");
}
