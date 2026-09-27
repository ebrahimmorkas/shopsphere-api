using FluentValidation;
using Microsoft.EntityFrameworkCore;
using ShopSphere.Application.Abstractions.Data;
using ShopSphere.Application.Abstractions.Messaging;
using ShopSphere.Domain.Abstractions;
using ShopSphere.Domain.Categories;

namespace ShopSphere.Application.Categories.Create;

public sealed record CreateCategoryCommand(string Name, string? Description) : ICommand<Guid>;

internal sealed class CreateCategoryCommandValidator : AbstractValidator<CreateCategoryCommand>
{
    public CreateCategoryCommandValidator()
    {
        RuleFor(c => c.Name).NotEmpty().MaximumLength(100);
        RuleFor(c => c.Description).MaximumLength(500);
    }
}

internal sealed class CreateCategoryCommandHandler(IApplicationDbContext dbContext)
    : ICommandHandler<CreateCategoryCommand, Guid>
{
    public async Task<Result<Guid>> Handle(CreateCategoryCommand command, CancellationToken cancellationToken)
    {
        var categoryResult = Category.Create(command.Name, command.Description);
        if (categoryResult.IsFailure)
        {
            return Result.Failure<Guid>(categoryResult.Error);
        }

        var category = categoryResult.Value;

        if (await dbContext.Categories.AnyAsync(c => c.Slug == category.Slug, cancellationToken))
        {
            return Result.Failure<Guid>(CategoryErrors.SlugNotUnique);
        }

        dbContext.Categories.Add(category);
        await dbContext.SaveChangesAsync(cancellationToken);

        return category.Id;
    }
}
