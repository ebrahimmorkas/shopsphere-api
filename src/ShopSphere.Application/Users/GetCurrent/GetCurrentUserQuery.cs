using Microsoft.EntityFrameworkCore;
using ShopSphere.Application.Abstractions.Authentication;
using ShopSphere.Application.Abstractions.Data;
using ShopSphere.Application.Abstractions.Messaging;
using ShopSphere.Domain.Abstractions;
using ShopSphere.Domain.Users;

namespace ShopSphere.Application.Users.GetCurrent;

public sealed record GetCurrentUserQuery : IQuery<UserResponse>;

public sealed record UserResponse(Guid Id, string Email, string FirstName, string LastName, string Role);

internal sealed class GetCurrentUserQueryHandler(IApplicationDbContext dbContext, IUserContext userContext)
    : IQueryHandler<GetCurrentUserQuery, UserResponse>
{
    public async Task<Result<UserResponse>> Handle(GetCurrentUserQuery query, CancellationToken cancellationToken)
    {
        var user = await dbContext.Users
            .AsNoTracking()
            .Where(u => u.Id == userContext.UserId)
            .Select(u => new UserResponse(u.Id, u.Email, u.FirstName, u.LastName, u.Role.ToString()))
            .SingleOrDefaultAsync(cancellationToken);

        return user is null
            ? Result.Failure<UserResponse>(UserErrors.NotFound(userContext.UserId))
            : user;
    }
}
