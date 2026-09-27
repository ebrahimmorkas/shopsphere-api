using FluentValidation;
using Microsoft.EntityFrameworkCore;
using ShopSphere.Application.Abstractions.Authentication;
using ShopSphere.Application.Abstractions.Data;
using ShopSphere.Application.Abstractions.Messaging;
using ShopSphere.Domain.Abstractions;
using ShopSphere.Domain.Users;

namespace ShopSphere.Application.Users.Refresh;

public sealed record RefreshTokenCommand(string RefreshToken) : ICommand<TokenResponse>;

internal sealed class RefreshTokenCommandValidator : AbstractValidator<RefreshTokenCommand>
{
    public RefreshTokenCommandValidator() => RuleFor(c => c.RefreshToken).NotEmpty();
}

internal sealed class RefreshTokenCommandHandler(
    IApplicationDbContext dbContext,
    ITokenProvider tokenProvider,
    TimeProvider timeProvider)
    : ICommandHandler<RefreshTokenCommand, TokenResponse>
{
    public async Task<Result<TokenResponse>> Handle(RefreshTokenCommand command, CancellationToken cancellationToken)
    {
        var utcNow = timeProvider.GetUtcNow().UtcDateTime;

        var existing = await dbContext.RefreshTokens
            .SingleOrDefaultAsync(t => t.Token == command.RefreshToken, cancellationToken);

        if (existing is null)
        {
            return Result.Failure<TokenResponse>(UserErrors.InvalidRefreshToken);
        }

        if (!existing.IsActive(utcNow))
        {
            if (existing.RevokedAtUtc is not null)
            {
                // A rotated token was presented again: assume it was stolen and revoke the whole family.
                await dbContext.RefreshTokens
                    .Where(t => t.UserId == existing.UserId && t.RevokedAtUtc == null)
                    .ExecuteUpdateAsync(s => s.SetProperty(t => t.RevokedAtUtc, utcNow), cancellationToken);
            }

            return Result.Failure<TokenResponse>(UserErrors.InvalidRefreshToken);
        }

        var user = await dbContext.Users.SingleOrDefaultAsync(u => u.Id == existing.UserId, cancellationToken);
        if (user is null)
        {
            return Result.Failure<TokenResponse>(UserErrors.InvalidRefreshToken);
        }

        existing.Revoke(utcNow);
        var response = TokenIssuer.Issue(user, tokenProvider, dbContext, utcNow);

        await dbContext.SaveChangesAsync(cancellationToken);
        return response;
    }
}
