using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using ShopSphere.Application.Abstractions.Messaging;
using ShopSphere.Domain.Abstractions;

namespace ShopSphere.Infrastructure.DomainEvents;

public interface IDomainEventsDispatcher
{
    Task DispatchAsync(IEnumerable<IDomainEvent> domainEvents, CancellationToken cancellationToken = default);
}

internal sealed class DomainEventsDispatcher(IServiceProvider serviceProvider) : IDomainEventsDispatcher
{
    private static readonly ConcurrentDictionary<Type, Type> HandlerTypeCache = new();
    private static readonly ConcurrentDictionary<Type, Type> WrapperTypeCache = new();

    public async Task DispatchAsync(IEnumerable<IDomainEvent> domainEvents, CancellationToken cancellationToken = default)
    {
        foreach (var domainEvent in domainEvents)
        {
            var eventType = domainEvent.GetType();
            var handlerType = HandlerTypeCache.GetOrAdd(eventType, t => typeof(IDomainEventHandler<>).MakeGenericType(t));

            foreach (var handler in serviceProvider.GetServices(handlerType))
            {
                if (handler is null)
                {
                    continue;
                }

                var wrapper = HandlerWrapper.Create(handler, eventType);
                await wrapper.Handle(domainEvent, cancellationToken);
            }
        }
    }

    // Small generic wrapper avoids `dynamic` and keeps the dispatch strongly typed.
    private abstract class HandlerWrapper
    {
        public abstract Task Handle(IDomainEvent domainEvent, CancellationToken cancellationToken);

        public static HandlerWrapper Create(object handler, Type eventType)
        {
            var wrapperType = WrapperTypeCache.GetOrAdd(eventType, t => typeof(HandlerWrapper<>).MakeGenericType(t));
            return (HandlerWrapper)Activator.CreateInstance(wrapperType, handler)!;
        }
    }

    private sealed class HandlerWrapper<T>(object handler) : HandlerWrapper
        where T : IDomainEvent
    {
        private readonly IDomainEventHandler<T> _handler = (IDomainEventHandler<T>)handler;

        public override Task Handle(IDomainEvent domainEvent, CancellationToken cancellationToken) =>
            _handler.Handle((T)domainEvent, cancellationToken);
    }
}
