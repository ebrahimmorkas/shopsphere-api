using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.JsonWebTokens;
using ShopSphere.Application.Abstractions.Authentication;
using ShopSphere.Domain.Users;

namespace ShopSphere.Infrastructure.Authentication;

internal sealed class UserContext(IHttpContextAccessor httpContextAccessor) : IUserContext
{
    private ClaimsPrincipal User =>
        httpContextAccessor.HttpContext?.User
        ?? throw new InvalidOperationException("User context is only available during an HTTP request.");

    public Guid UserId =>
        Guid.TryParse(User.FindFirstValue(JwtRegisteredClaimNames.Sub), out var id)
            ? id
            : throw new InvalidOperationException("The current user is not authenticated.");

    public bool IsAdmin => User.IsInRole(nameof(Role.Admin));
}
