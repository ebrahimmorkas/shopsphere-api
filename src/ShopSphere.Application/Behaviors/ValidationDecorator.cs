using FluentValidation;
using FluentValidation.Results;
using ShopSphere.Application.Abstractions.Messaging;
using ShopSphere.Domain.Abstractions;

namespace ShopSphere.Application.Behaviors;

internal static class ValidationDecorator
{
    internal sealed class CommandHandler<TCommand, TResponse>(
        ICommandHandler<TCommand, TResponse> innerHandler,
        IEnumerable<IValidator<TCommand>> validators)
        : ICommandHandler<TCommand, TResponse>
        where TCommand : ICommand<TResponse>
    {
        public async Task<Result<TResponse>> Handle(TCommand command, CancellationToken cancellationToken)
        {
            var failures = await ValidateAsync(command, validators, cancellationToken);

            return failures.Length == 0
                ? await innerHandler.Handle(command, cancellationToken)
                : Result.Failure<TResponse>(CreateValidationError(failures));
        }
    }

    internal sealed class CommandBaseHandler<TCommand>(
        ICommandHandler<TCommand> innerHandler,
        IEnumerable<IValidator<TCommand>> validators)
        : ICommandHandler<TCommand>
        where TCommand : ICommand
    {
        public async Task<Result> Handle(TCommand command, CancellationToken cancellationToken)
        {
            var failures = await ValidateAsync(command, validators, cancellationToken);

            return failures.Length == 0
                ? await innerHandler.Handle(command, cancellationToken)
                : Result.Failure(CreateValidationError(failures));
        }
    }

    internal sealed class QueryHandler<TQuery, TResponse>(
        IQueryHandler<TQuery, TResponse> innerHandler,
        IEnumerable<IValidator<TQuery>> validators)
        : IQueryHandler<TQuery, TResponse>
        where TQuery : IQuery<TResponse>
    {
        public async Task<Result<TResponse>> Handle(TQuery query, CancellationToken cancellationToken)
        {
            var failures = await ValidateAsync(query, validators, cancellationToken);

            return failures.Length == 0
                ? await innerHandler.Handle(query, cancellationToken)
                : Result.Failure<TResponse>(CreateValidationError(failures));
        }
    }

    private static async Task<ValidationFailure[]> ValidateAsync<T>(
        T request,
        IEnumerable<IValidator<T>> validators,
        CancellationToken cancellationToken)
    {
        var validatorList = validators as IValidator<T>[] ?? validators.ToArray();
        if (validatorList.Length == 0)
        {
            return [];
        }

        var context = new ValidationContext<T>(request);
        var results = await Task.WhenAll(validatorList.Select(v => v.ValidateAsync(context, cancellationToken)));

        return results
            .Where(r => !r.IsValid)
            .SelectMany(r => r.Errors)
            .ToArray();
    }

    private static ValidationError CreateValidationError(ValidationFailure[] failures) =>
        new(failures.Select(f => Error.Validation(f.PropertyName, f.ErrorMessage)).ToArray());
}
