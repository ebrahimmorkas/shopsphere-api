using FluentValidation;
using Microsoft.EntityFrameworkCore;
using ShopSphere.Application.Abstractions.Data;
using ShopSphere.Application.Abstractions.Messaging;
using ShopSphere.Domain.Abstractions;
using ShopSphere.Domain.Products;

namespace ShopSphere.Application.Products.Restock;

public sealed record RestockProductCommand(Guid ProductId, int Quantity) : ICommand;

internal sealed class RestockProductCommandValidator : AbstractValidator<RestockProductCommand>
{
    public RestockProductCommandValidator()
    {
        RuleFor(c => c.ProductId).NotEmpty();
        RuleFor(c => c.Quantity).InclusiveBetween(1, 10_000);
    }
}

internal sealed class RestockProductCommandHandler(IApplicationDbContext dbContext, TimeProvider timeProvider)
    : ICommandHandler<RestockProductCommand>
{
    public async Task<Result> Handle(RestockProductCommand command, CancellationToken cancellationToken)
    {
        var product = await dbContext.Products.SingleOrDefaultAsync(p => p.Id == command.ProductId, cancellationToken);
        if (product is null)
        {
            return ProductErrors.NotFound(command.ProductId);
        }

        var result = product.AddStock(command.Quantity, timeProvider.GetUtcNow().UtcDateTime);
        if (result.IsFailure)
        {
            return result;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
