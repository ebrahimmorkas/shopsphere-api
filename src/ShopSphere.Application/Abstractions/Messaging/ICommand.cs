namespace ShopSphere.Application.Abstractions.Messaging;

/// <summary>
/// Marker for any command. Used by pipeline decorators to identify state-changing requests.
/// </summary>
public interface IBaseCommand;

public interface ICommand : IBaseCommand;

public interface ICommand<TResponse> : IBaseCommand;
