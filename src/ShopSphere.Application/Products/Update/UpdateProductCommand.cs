using FluentValidation;
using Microsoft.EntityFrameworkCore;
using ShopSphere.Application.Abstractions.Data;
using ShopSphere.Application.Abstractions.Messaging;
using ShopSphere.Domain.Abstractions;
using ShopSphere.Domain.Categories;
using ShopSphere.Domain.Products;
using ShopSphere.Domain.Shared;

namespace ShopSphere.Application.Products.Update;

public sealed record UpdateProductCommand(
    Guid ProductId,
    string Name,
    string Description,
    decimal Price,
    string Currency,
    Guid CategoryId) : ICommand;

internal sealed class UpdateProductCommandValidator : AbstractValidator<UpdateProductCommand>
{
    public UpdateProductCommandValidator()
    {
        RuleFor(c => c.ProductId).NotEmpty();
        RuleFor(c => c.Name).NotEmpty().MaximumLength(200);
        RuleFor(c => c.Description).MaximumLength(2000);
        RuleFor(c => c.Price).GreaterThan(0);
        RuleFor(c => c.Currency).NotEmpty().Length(3);
        RuleFor(c => c.CategoryId).NotEmpty();
    }
}

internal sealed class UpdateProductCommandHandler(IApplicationDbContext dbContext, TimeProvider timeProvider)
    : ICommandHandler<UpdateProductCommand>
{
    public async Task<Result> Handle(UpdateProductCommand command, CancellationToken cancellationToken)
    {
        var product = await dbContext.Products.SingleOrDefaultAsync(p => p.Id == command.ProductId, cancellationToken);
        if (product is null)
        {
            return ProductErrors.NotFound(command.ProductId);
        }

        if (!await dbContext.Categories.AnyAsync(c => c.Id == command.CategoryId, cancellationToken))
        {
            return CategoryErrors.NotFound(command.CategoryId);
        }

        var priceResult = Money.Create(command.Price, command.Currency);
        if (priceResult.IsFailure)
        {
            return priceResult;
        }

        var utcNow = timeProvider.GetUtcNow().UtcDateTime;

        var updateResult = product.UpdateDetails(command.Name, command.Description, command.CategoryId, utcNow);
        if (updateResult.IsFailure)
        {
            return updateResult;
        }

        product.ChangePrice(priceResult.Value, utcNow);

        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
