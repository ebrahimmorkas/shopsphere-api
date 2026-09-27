using ShopSphere.Api.Extensions;
using ShopSphere.Application.Abstractions.Messaging;
using ShopSphere.Application.Common;
using ShopSphere.Application.Products;
using ShopSphere.Application.Products.Create;
using ShopSphere.Application.Products.Deactivate;
using ShopSphere.Application.Products.GetById;
using ShopSphere.Application.Products.Restock;
using ShopSphere.Application.Products.Search;
using ShopSphere.Application.Products.Update;

namespace ShopSphere.Api.Endpoints.Products;

internal sealed class ProductEndpoints : IEndpoint
{
    public sealed record CreateProductRequest(
        string Name,
        string Description,
        string Sku,
        decimal Price,
        string Currency,
        int StockQuantity,
        Guid CategoryId);

    public sealed record UpdateProductRequest(
        string Name,
        string Description,
        decimal Price,
        string Currency,
        Guid CategoryId);

    public sealed record RestockRequest(int Quantity);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("products").WithTags(Tags.Products);

        group.MapGet("/", async (
                [AsParameters] SearchProductsRequest request,
                IQueryHandler<SearchProductsQuery, PagedList<ProductResponse>> handler,
                CancellationToken cancellationToken) =>
            {
                var query = new SearchProductsQuery(
                    request.Search,
                    request.CategoryId,
                    request.MinPrice,
                    request.MaxPrice,
                    request.IncludeInactive ?? false,
                    request.SortBy,
                    request.SortOrder,
                    request.Page ?? 1,
                    request.PageSize ?? 20);

                var result = await handler.Handle(query, cancellationToken);
                return result.Match(Results.Ok);
            })
            .WithName("SearchProducts")
            .WithSummary("Searches products with filtering, sorting and pagination")
            .Produces<PagedList<ProductResponse>>()
            .ProducesProblem(StatusCodes.Status400BadRequest);

        group.MapGet("/{id:guid}", async (
                Guid id,
                IQueryHandler<GetProductByIdQuery, ProductResponse> handler,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.Handle(new GetProductByIdQuery(id), cancellationToken);
                return result.Match(Results.Ok);
            })
            .WithName("GetProductById")
            .WithSummary("Gets a product by id")
            .Produces<ProductResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPost("/", async (
                CreateProductRequest request,
                ICommandHandler<CreateProductCommand, Guid> handler,
                CancellationToken cancellationToken) =>
            {
                var command = new CreateProductCommand(
                    request.Name,
                    request.Description,
                    request.Sku,
                    request.Price,
                    request.Currency,
                    request.StockQuantity,
                    request.CategoryId);

                var result = await handler.Handle(command, cancellationToken);
                return result.Match(id => Results.CreatedAtRoute("GetProductById", new { id }, new { id }));
            })
            .WithName("CreateProduct")
            .WithSummary("Creates a product")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPut("/{id:guid}", async (
                Guid id,
                UpdateProductRequest request,
                ICommandHandler<UpdateProductCommand> handler,
                CancellationToken cancellationToken) =>
            {
                var command = new UpdateProductCommand(
                    id, request.Name, request.Description, request.Price, request.Currency, request.CategoryId);

                var result = await handler.Handle(command, cancellationToken);
                return result.Match(Results.NoContent);
            })
            .WithName("UpdateProduct")
            .WithSummary("Updates product details and price")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPost("/{id:guid}/restock", async (
                Guid id,
                RestockRequest request,
                ICommandHandler<RestockProductCommand> handler,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.Handle(new RestockProductCommand(id, request.Quantity), cancellationToken);
                return result.Match(Results.NoContent);
            })
            .WithName("RestockProduct")
            .WithSummary("Adds stock to a product")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapDelete("/{id:guid}", async (
                Guid id,
                ICommandHandler<DeactivateProductCommand> handler,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.Handle(new DeactivateProductCommand(id), cancellationToken);
                return result.Match(Results.NoContent);
            })
            .WithName("DeactivateProduct")
            .WithSummary("Soft-deletes a product by deactivating it")
            .ProducesProblem(StatusCodes.Status404NotFound);
    }

    internal sealed record SearchProductsRequest(
        string? Search,
        Guid? CategoryId,
        decimal? MinPrice,
        decimal? MaxPrice,
        bool? IncludeInactive,
        string? SortBy,
        string? SortOrder,
        int? Page,
        int? PageSize);
}
