using ShopSphere.Domain.Abstractions;

namespace ShopSphere.Domain.Users;

public sealed class User : Entity, IAuditable
{
    private User(Guid id, string email, string firstName, string lastName, string passwordHash, Role role, DateTime createdAtUtc)
        : base(id)
    {
        Email = email;
        FirstName = firstName;
        LastName = lastName;
        PasswordHash = passwordHash;
        Role = role;
        CreatedAtUtc = createdAtUtc;
    }

    private User()
    {
    }

    public string Email { get; private set; } = string.Empty;

    public string FirstName { get; private set; } = string.Empty;

    public string LastName { get; private set; } = string.Empty;

    public string PasswordHash { get; private set; } = string.Empty;

    public Role Role { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public DateTime? UpdatedAtUtc { get; private set; }

    public string FullName => $"{FirstName} {LastName}";

    public static User Create(
        string email,
        string firstName,
        string lastName,
        string passwordHash,
        DateTime utcNow,
        Role role = Role.Customer)
    {
        var user = new User(
            Guid.CreateVersion7(),
            NormalizeEmail(email),
            firstName.Trim(),
            lastName.Trim(),
            passwordHash,
            role,
            utcNow);

        user.Raise(new UserRegisteredDomainEvent(user.Id));

        return user;
    }

    public static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();
}

public enum Role
{
    Customer = 0,
    Admin = 1
}

public sealed record UserRegisteredDomainEvent(Guid UserId) : IDomainEvent;
