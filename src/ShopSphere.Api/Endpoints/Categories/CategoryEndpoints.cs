using ShopSphere.Api.Extensions;
using ShopSphere.Application.Abstractions.Messaging;
using ShopSphere.Application.Categories;
using ShopSphere.Application.Categories.Create;
using ShopSphere.Application.Categories.Delete;
using ShopSphere.Application.Categories.GetAll;
using ShopSphere.Application.Categories.Update;

namespace ShopSphere.Api.Endpoints.Categories;

internal sealed class CategoryEndpoints : IEndpoint
{
    public sealed record CategoryRequest(string Name, string? Description);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("categories").WithTags(Tags.Categories);

        group.MapGet("/", async (
                IQueryHandler<GetCategoriesQuery, IReadOnlyList<CategoryResponse>> handler,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.Handle(new GetCategoriesQuery(), cancellationToken);
                return result.Match(Results.Ok);
            })
            .WithName("GetCategories")
            .WithSummary("Lists all categories")
            .Produces<IReadOnlyList<CategoryResponse>>();

        group.MapPost("/", async (
                CategoryRequest request,
                ICommandHandler<CreateCategoryCommand, Guid> handler,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.Handle(new CreateCategoryCommand(request.Name, request.Description), cancellationToken);
                return result.Match(id => Results.Created($"/api/v1/categories/{id}", new { id }));
            })
            .WithName("CreateCategory")
            .WithSummary("Creates a category")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPut("/{id:guid}", async (
                Guid id,
                CategoryRequest request,
                ICommandHandler<UpdateCategoryCommand> handler,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.Handle(new UpdateCategoryCommand(id, request.Name, request.Description), cancellationToken);
                return result.Match(Results.NoContent);
            })
            .WithName("UpdateCategory")
            .WithSummary("Updates a category")
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapDelete("/{id:guid}", async (
                Guid id,
                ICommandHandler<DeleteCategoryCommand> handler,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.Handle(new DeleteCategoryCommand(id), cancellationToken);
                return result.Match(Results.NoContent);
            })
            .WithName("DeleteCategory")
            .WithSummary("Deletes an empty category")
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
    }
}
