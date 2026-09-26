namespace ShopSphere.Domain.Shared;

public sealed record Address(
    string Street,
    string City,
    string State,
    string PostalCode,
    string Country);
