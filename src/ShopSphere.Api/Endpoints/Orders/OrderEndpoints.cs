using ShopSphere.Api.Extensions;
using ShopSphere.Application.Abstractions.Messaging;
using ShopSphere.Application.Common;
using ShopSphere.Application.Orders;
using ShopSphere.Application.Orders.Cancel;
using ShopSphere.Application.Orders.ChangeStatus;
using ShopSphere.Application.Orders.GetById;
using ShopSphere.Application.Orders.GetMine;
using ShopSphere.Application.Orders.Place;
using ShopSphere.Infrastructure.Authentication;

namespace ShopSphere.Api.Endpoints.Orders;

internal sealed class OrderEndpoints : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("orders")
            .WithTags(Tags.Orders)
            .RequireAuthorization();

        group.MapPost("/", async (
                PlaceOrderCommand command,
                ICommandHandler<PlaceOrderCommand, Guid> handler,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.Handle(command, cancellationToken);
                return result.Match(id => Results.CreatedAtRoute("GetOrderById", new { id }, new { id }));
            })
            .WithName("PlaceOrder")
            .WithSummary("Places an order and reserves stock")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapGet("/", async (
                int? page,
                int? pageSize,
                IQueryHandler<GetMyOrdersQuery, PagedList<OrderSummaryResponse>> handler,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.Handle(new GetMyOrdersQuery(page ?? 1, pageSize ?? 20), cancellationToken);
                return result.Match(Results.Ok);
            })
            .WithName("GetMyOrders")
            .WithSummary("Lists the current customer's orders")
            .Produces<PagedList<OrderSummaryResponse>>();

        group.MapGet("/{id:guid}", async (
                Guid id,
                IQueryHandler<GetOrderByIdQuery, OrderResponse> handler,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.Handle(new GetOrderByIdQuery(id), cancellationToken);
                return result.Match(Results.Ok);
            })
            .WithName("GetOrderById")
            .WithSummary("Gets an order (own orders only, admins can see all)")
            .Produces<OrderResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPost("/{id:guid}/cancel", async (
                Guid id,
                ICommandHandler<CancelOrderCommand> handler,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.Handle(new CancelOrderCommand(id), cancellationToken);
                return result.Match(Results.NoContent);
            })
            .WithName("CancelOrder")
            .WithSummary("Cancels an order that has not shipped and restocks its items")
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        MapTransition(group, "pay", OrderStatusTransition.MarkAsPaid, "MarkOrderAsPaid");
        MapTransition(group, "ship", OrderStatusTransition.Ship, "ShipOrder");
        MapTransition(group, "deliver", OrderStatusTransition.Deliver, "DeliverOrder");
    }

    private static void MapTransition(RouteGroupBuilder group, string action, OrderStatusTransition transition, string name) =>
        group.MapPost($"/{{id:guid}}/{action}", async (
                Guid id,
                ICommandHandler<ChangeOrderStatusCommand> handler,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.Handle(new ChangeOrderStatusCommand(id, transition), cancellationToken);
                return result.Match(Results.NoContent);
            })
            .RequireAuthorization(Policies.Admin)
            .WithName(name)
            .WithSummary($"Admin: {action} an order")
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
}
