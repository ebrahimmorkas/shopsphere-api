using FluentValidation;
using NSubstitute;
using ShopSphere.Application.Abstractions.Messaging;
using ShopSphere.Application.Behaviors;
using ShopSphere.Domain.Abstractions;

namespace ShopSphere.Application.UnitTests.Behaviors;

public class ValidationDecoratorTests
{
    public sealed record TestCommand(string Name) : ICommand<Guid>;

    private sealed class TestCommandValidator : AbstractValidator<TestCommand>
    {
        public TestCommandValidator() => RuleFor(c => c.Name).NotEmpty();
    }

    private readonly ICommandHandler<TestCommand, Guid> _inner = Substitute.For<ICommandHandler<TestCommand, Guid>>();

    [Fact]
    public async Task Handle_Should_Return_ValidationError_And_Skip_Handler_When_Invalid()
    {
        var sut = new ValidationDecorator.CommandHandler<TestCommand, Guid>(_inner, [new TestCommandValidator()]);

        var result = await sut.Handle(new TestCommand(string.Empty), TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        var error = result.Error.ShouldBeOfType<ValidationError>();
        error.Errors.ShouldHaveSingleItem().Code.ShouldBe(nameof(TestCommand.Name));
        await _inner.DidNotReceiveWithAnyArgs().Handle(default!, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Handle_Should_Invoke_Handler_When_Valid()
    {
        var id = Guid.NewGuid();
        var command = new TestCommand("valid");
        _inner.Handle(command, Arg.Any<CancellationToken>()).Returns(Result.Success(id));
        var sut = new ValidationDecorator.CommandHandler<TestCommand, Guid>(_inner, [new TestCommandValidator()]);

        var result = await sut.Handle(command, TestContext.Current.CancellationToken);

        result.Value.ShouldBe(id);
    }

    [Fact]
    public async Task Handle_Should_Invoke_Handler_When_No_Validators_Registered()
    {
        var command = new TestCommand(string.Empty);
        _inner.Handle(command, Arg.Any<CancellationToken>()).Returns(Result.Success(Guid.Empty));
        var sut = new ValidationDecorator.CommandHandler<TestCommand, Guid>(_inner, []);

        var result = await sut.Handle(command, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
    }
}
