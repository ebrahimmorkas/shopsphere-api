using Microsoft.EntityFrameworkCore;
using ShopSphere.Application.Abstractions.Authentication;
using ShopSphere.Application.Abstractions.Data;
using ShopSphere.Application.Abstractions.Messaging;
using ShopSphere.Domain.Abstractions;
using ShopSphere.Domain.Orders;

namespace ShopSphere.Application.Orders.Cancel;

public sealed record CancelOrderCommand(Guid OrderId) : ICommand;

internal sealed class CancelOrderCommandHandler(
    IApplicationDbContext dbContext,
    IUserContext userContext,
    TimeProvider timeProvider)
    : ICommandHandler<CancelOrderCommand>
{
    public async Task<Result> Handle(CancelOrderCommand command, CancellationToken cancellationToken)
    {
        var order = await dbContext.Orders
            .Include(o => o.Items)
            .VisibleTo(userContext)
            .SingleOrDefaultAsync(o => o.Id == command.OrderId, cancellationToken);

        if (order is null)
        {
            return OrderErrors.NotFound(command.OrderId);
        }

        var result = order.Cancel(timeProvider.GetUtcNow().UtcDateTime);
        if (result.IsFailure)
        {
            return result;
        }

        // OrderCancelledDomainEvent is handled before commit and returns the reserved stock.
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
