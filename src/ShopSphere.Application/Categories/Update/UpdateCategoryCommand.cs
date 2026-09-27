using FluentValidation;
using Microsoft.EntityFrameworkCore;
using ShopSphere.Application.Abstractions.Data;
using ShopSphere.Application.Abstractions.Messaging;
using ShopSphere.Domain.Abstractions;
using ShopSphere.Domain.Categories;

namespace ShopSphere.Application.Categories.Update;

public sealed record UpdateCategoryCommand(Guid CategoryId, string Name, string? Description) : ICommand;

internal sealed class UpdateCategoryCommandValidator : AbstractValidator<UpdateCategoryCommand>
{
    public UpdateCategoryCommandValidator()
    {
        RuleFor(c => c.CategoryId).NotEmpty();
        RuleFor(c => c.Name).NotEmpty().MaximumLength(100);
        RuleFor(c => c.Description).MaximumLength(500);
    }
}

internal sealed class UpdateCategoryCommandHandler(IApplicationDbContext dbContext)
    : ICommandHandler<UpdateCategoryCommand>
{
    public async Task<Result> Handle(UpdateCategoryCommand command, CancellationToken cancellationToken)
    {
        var category = await dbContext.Categories.SingleOrDefaultAsync(c => c.Id == command.CategoryId, cancellationToken);
        if (category is null)
        {
            return CategoryErrors.NotFound(command.CategoryId);
        }

        var slug = Category.ToSlug(command.Name);
        if (await dbContext.Categories.AnyAsync(c => c.Slug == slug && c.Id != command.CategoryId, cancellationToken))
        {
            return CategoryErrors.SlugNotUnique;
        }

        var result = category.Update(command.Name, command.Description);
        if (result.IsFailure)
        {
            return result;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
