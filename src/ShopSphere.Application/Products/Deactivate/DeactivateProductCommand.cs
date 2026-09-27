using Microsoft.EntityFrameworkCore;
using ShopSphere.Application.Abstractions.Data;
using ShopSphere.Application.Abstractions.Messaging;
using ShopSphere.Domain.Abstractions;
using ShopSphere.Domain.Products;

namespace ShopSphere.Application.Products.Deactivate;

public sealed record DeactivateProductCommand(Guid ProductId) : ICommand;

internal sealed class DeactivateProductCommandHandler(IApplicationDbContext dbContext, TimeProvider timeProvider)
    : ICommandHandler<DeactivateProductCommand>
{
    public async Task<Result> Handle(DeactivateProductCommand command, CancellationToken cancellationToken)
    {
        var product = await dbContext.Products.SingleOrDefaultAsync(p => p.Id == command.ProductId, cancellationToken);
        if (product is null)
        {
            return ProductErrors.NotFound(command.ProductId);
        }

        product.Deactivate(timeProvider.GetUtcNow().UtcDateTime);

        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
