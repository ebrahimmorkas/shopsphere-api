using Microsoft.EntityFrameworkCore;
using ShopSphere.Application.Abstractions.Data;
using ShopSphere.Application.Abstractions.Messaging;
using ShopSphere.Domain.Abstractions;
using ShopSphere.Domain.Orders;

namespace ShopSphere.Application.Orders.ChangeStatus;

public enum OrderStatusTransition
{
    MarkAsPaid,
    Ship,
    Deliver
}

public sealed record ChangeOrderStatusCommand(Guid OrderId, OrderStatusTransition Transition) : ICommand;

internal sealed class ChangeOrderStatusCommandHandler(IApplicationDbContext dbContext, TimeProvider timeProvider)
    : ICommandHandler<ChangeOrderStatusCommand>
{
    public async Task<Result> Handle(ChangeOrderStatusCommand command, CancellationToken cancellationToken)
    {
        var order = await dbContext.Orders.SingleOrDefaultAsync(o => o.Id == command.OrderId, cancellationToken);
        if (order is null)
        {
            return OrderErrors.NotFound(command.OrderId);
        }

        var utcNow = timeProvider.GetUtcNow().UtcDateTime;

        var result = command.Transition switch
        {
            OrderStatusTransition.MarkAsPaid => order.MarkAsPaid(utcNow),
            OrderStatusTransition.Ship => order.Ship(utcNow),
            OrderStatusTransition.Deliver => order.Deliver(utcNow),
            _ => throw new ArgumentOutOfRangeException(nameof(command), command.Transition, "Unknown transition.")
        };

        if (result.IsFailure)
        {
            return result;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
