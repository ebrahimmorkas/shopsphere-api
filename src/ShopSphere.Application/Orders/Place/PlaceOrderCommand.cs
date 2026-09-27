using FluentValidation;
using Microsoft.EntityFrameworkCore;
using ShopSphere.Application.Abstractions.Authentication;
using ShopSphere.Application.Abstractions.Data;
using ShopSphere.Application.Abstractions.Messaging;
using ShopSphere.Domain.Abstractions;
using ShopSphere.Domain.Orders;
using ShopSphere.Domain.Products;
using ShopSphere.Domain.Shared;

namespace ShopSphere.Application.Orders.Place;

public sealed record PlaceOrderCommand(AddressRequest ShippingAddress, IReadOnlyList<OrderLineRequest> Items) : ICommand<Guid>;

public sealed record AddressRequest(string Street, string City, string State, string PostalCode, string Country);

public sealed record OrderLineRequest(Guid ProductId, int Quantity);

internal sealed class PlaceOrderCommandValidator : AbstractValidator<PlaceOrderCommand>
{
    public PlaceOrderCommandValidator()
    {
        RuleFor(c => c.ShippingAddress).NotNull();
        RuleFor(c => c.ShippingAddress.Street).NotEmpty().MaximumLength(200);
        RuleFor(c => c.ShippingAddress.City).NotEmpty().MaximumLength(100);
        RuleFor(c => c.ShippingAddress.State).MaximumLength(100);
        RuleFor(c => c.ShippingAddress.PostalCode).NotEmpty().MaximumLength(20);
        RuleFor(c => c.ShippingAddress.Country).NotEmpty().Length(2).WithMessage("Country must be an ISO 3166-1 alpha-2 code.");

        RuleFor(c => c.Items).NotEmpty().Must(items => items.Count <= 50).WithMessage("An order can contain at most 50 lines.");
        RuleForEach(c => c.Items).ChildRules(line =>
        {
            line.RuleFor(l => l.ProductId).NotEmpty();
            line.RuleFor(l => l.Quantity).InclusiveBetween(1, 100);
        });
    }
}

internal sealed class PlaceOrderCommandHandler(
    IApplicationDbContext dbContext,
    IUserContext userContext,
    TimeProvider timeProvider)
    : ICommandHandler<PlaceOrderCommand, Guid>
{
    public async Task<Result<Guid>> Handle(PlaceOrderCommand command, CancellationToken cancellationToken)
    {
        var utcNow = timeProvider.GetUtcNow().UtcDateTime;
        var productIds = command.Items.Select(i => i.ProductId).Distinct().ToList();

        var products = await dbContext.Products
            .Where(p => productIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, cancellationToken);

        var missing = productIds.FirstOrDefault(id => !products.ContainsKey(id));
        if (missing != Guid.Empty)
        {
            return Result.Failure<Guid>(ProductErrors.NotFound(missing));
        }

        var address = new Address(
            command.ShippingAddress.Street,
            command.ShippingAddress.City,
            command.ShippingAddress.State,
            command.ShippingAddress.PostalCode,
            command.ShippingAddress.Country.ToUpperInvariant());

        var order = Order.Create(userContext.UserId, address, utcNow);

        foreach (var line in command.Items)
        {
            var addResult = order.AddItem(products[line.ProductId], line.Quantity, utcNow);
            if (addResult.IsFailure)
            {
                return Result.Failure<Guid>(addResult.Error);
            }
        }

        var placeResult = order.Place(utcNow);
        if (placeResult.IsFailure)
        {
            return Result.Failure<Guid>(placeResult.Error);
        }

        dbContext.Orders.Add(order);

        // Product rows carry an xmin concurrency token: if another order reserved the same stock
        // concurrently, SaveChanges throws and the API responds with 409 so the client can retry.
        await dbContext.SaveChangesAsync(cancellationToken);

        return order.Id;
    }
}
