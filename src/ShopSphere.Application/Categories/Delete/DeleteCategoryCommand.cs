using Microsoft.EntityFrameworkCore;
using ShopSphere.Application.Abstractions.Data;
using ShopSphere.Application.Abstractions.Messaging;
using ShopSphere.Domain.Abstractions;
using ShopSphere.Domain.Categories;

namespace ShopSphere.Application.Categories.Delete;

public sealed record DeleteCategoryCommand(Guid CategoryId) : ICommand;

internal sealed class DeleteCategoryCommandHandler(IApplicationDbContext dbContext)
    : ICommandHandler<DeleteCategoryCommand>
{
    public async Task<Result> Handle(DeleteCategoryCommand command, CancellationToken cancellationToken)
    {
        var category = await dbContext.Categories.SingleOrDefaultAsync(c => c.Id == command.CategoryId, cancellationToken);
        if (category is null)
        {
            return CategoryErrors.NotFound(command.CategoryId);
        }

        if (await dbContext.Products.AnyAsync(p => p.CategoryId == command.CategoryId, cancellationToken))
        {
            return CategoryErrors.HasProducts;
        }

        dbContext.Categories.Remove(category);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
