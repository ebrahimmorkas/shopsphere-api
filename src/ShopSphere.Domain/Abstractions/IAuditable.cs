namespace ShopSphere.Domain.Abstractions;

public interface IAuditable
{
    DateTime CreatedAtUtc { get; }

    DateTime? UpdatedAtUtc { get; }
}
