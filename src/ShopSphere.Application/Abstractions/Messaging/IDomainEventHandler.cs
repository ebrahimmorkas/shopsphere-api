using ShopSphere.Domain.Abstractions;

namespace ShopSphere.Application.Abstractions.Messaging;

public interface IDomainEventHandler<in TDomainEvent>
    where TDomainEvent : IDomainEvent
{
    Task Handle(TDomainEvent domainEvent, CancellationToken cancellationToken);
}
