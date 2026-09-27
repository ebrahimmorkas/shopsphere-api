using FluentValidation;
using Microsoft.EntityFrameworkCore;
using ShopSphere.Application.Abstractions.Data;
using ShopSphere.Application.Abstractions.Messaging;
using ShopSphere.Domain.Abstractions;
using ShopSphere.Domain.Categories;
using ShopSphere.Domain.Products;
using ShopSphere.Domain.Shared;

namespace ShopSphere.Application.Products.Create;

public sealed record CreateProductCommand(
    string Name,
    string Description,
    string Sku,
    decimal Price,
    string Currency,
    int StockQuantity,
    Guid CategoryId) : ICommand<Guid>;

internal sealed class CreateProductCommandValidator : AbstractValidator<CreateProductCommand>
{
    public CreateProductCommandValidator()
    {
        RuleFor(c => c.Name).NotEmpty().MaximumLength(200);
        RuleFor(c => c.Description).MaximumLength(2000);
        RuleFor(c => c.Sku).NotEmpty().Matches("^[A-Za-z0-9-]{4,32}$");
        RuleFor(c => c.Price).GreaterThan(0);
        RuleFor(c => c.Currency).NotEmpty().Length(3);
        RuleFor(c => c.StockQuantity).GreaterThanOrEqualTo(0);
        RuleFor(c => c.CategoryId).NotEmpty();
    }
}

internal sealed class CreateProductCommandHandler(IApplicationDbContext dbContext, TimeProvider timeProvider)
    : ICommandHandler<CreateProductCommand, Guid>
{
    public async Task<Result<Guid>> Handle(CreateProductCommand command, CancellationToken cancellationToken)
    {
        if (!await dbContext.Categories.AnyAsync(c => c.Id == command.CategoryId, cancellationToken))
        {
            return Result.Failure<Guid>(CategoryErrors.NotFound(command.CategoryId));
        }

        var skuResult = Sku.Create(command.Sku);
        if (skuResult.IsFailure)
        {
            return Result.Failure<Guid>(skuResult.Error);
        }

        if (await dbContext.Products.AnyAsync(p => p.Sku == skuResult.Value, cancellationToken))
        {
            return Result.Failure<Guid>(ProductErrors.SkuNotUnique);
        }

        var priceResult = Money.Create(command.Price, command.Currency);
        if (priceResult.IsFailure)
        {
            return Result.Failure<Guid>(priceResult.Error);
        }

        var productResult = Product.Create(
            command.Name,
            command.Description,
            skuResult.Value,
            priceResult.Value,
            command.StockQuantity,
            command.CategoryId,
            timeProvider.GetUtcNow().UtcDateTime);

        if (productResult.IsFailure)
        {
            return Result.Failure<Guid>(productResult.Error);
        }

        dbContext.Products.Add(productResult.Value);
        await dbContext.SaveChangesAsync(cancellationToken);

        return productResult.Value.Id;
    }
}
