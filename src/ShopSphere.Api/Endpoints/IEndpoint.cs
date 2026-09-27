namespace ShopSphere.Api.Endpoints;

/// <summary>
/// Implemented by each feature endpoint. Implementations are discovered at startup and mapped automatically.
/// </summary>
public interface IEndpoint
{
    void MapEndpoint(IEndpointRouteBuilder app);
}
