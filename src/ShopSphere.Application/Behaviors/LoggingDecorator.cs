using System.Diagnostics;
using Microsoft.Extensions.Logging;
using ShopSphere.Application.Abstractions.Messaging;
using ShopSphere.Domain.Abstractions;

namespace ShopSphere.Application.Behaviors;

internal static partial class LoggingDecorator
{
    internal sealed class CommandHandler<TCommand, TResponse>(
        ICommandHandler<TCommand, TResponse> innerHandler,
        ILogger<CommandHandler<TCommand, TResponse>> logger)
        : ICommandHandler<TCommand, TResponse>
        where TCommand : ICommand<TResponse>
    {
        public async Task<Result<TResponse>> Handle(TCommand command, CancellationToken cancellationToken)
        {
            var start = Stopwatch.GetTimestamp();
            var result = await innerHandler.Handle(command, cancellationToken);
            Log(logger, typeof(TCommand).Name, result, start);
            return result;
        }
    }

    internal sealed class CommandBaseHandler<TCommand>(
        ICommandHandler<TCommand> innerHandler,
        ILogger<CommandBaseHandler<TCommand>> logger)
        : ICommandHandler<TCommand>
        where TCommand : ICommand
    {
        public async Task<Result> Handle(TCommand command, CancellationToken cancellationToken)
        {
            var start = Stopwatch.GetTimestamp();
            var result = await innerHandler.Handle(command, cancellationToken);
            Log(logger, typeof(TCommand).Name, result, start);
            return result;
        }
    }

    internal sealed class QueryHandler<TQuery, TResponse>(
        IQueryHandler<TQuery, TResponse> innerHandler,
        ILogger<QueryHandler<TQuery, TResponse>> logger)
        : IQueryHandler<TQuery, TResponse>
        where TQuery : IQuery<TResponse>
    {
        public async Task<Result<TResponse>> Handle(TQuery query, CancellationToken cancellationToken)
        {
            var start = Stopwatch.GetTimestamp();
            var result = await innerHandler.Handle(query, cancellationToken);
            Log(logger, typeof(TQuery).Name, result, start);
            return result;
        }
    }

    private static void Log(ILogger logger, string requestName, Result result, long start)
    {
        var elapsedMs = Stopwatch.GetElapsedTime(start).TotalMilliseconds;

        if (result.IsSuccess)
        {
            LogCompleted(logger, requestName, elapsedMs);
        }
        else
        {
            LogFailed(logger, requestName, result.Error.Code, elapsedMs);
        }
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Request {RequestName} completed in {ElapsedMs:0.0} ms")]
    private static partial void LogCompleted(ILogger logger, string requestName, double elapsedMs);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Request {RequestName} failed with {ErrorCode} in {ElapsedMs:0.0} ms")]
    private static partial void LogFailed(ILogger logger, string requestName, string errorCode, double elapsedMs);
}
