using ShopSphere.Api.Extensions;
using ShopSphere.Application.Abstractions.Messaging;
using ShopSphere.Application.Users;
using ShopSphere.Application.Users.GetCurrent;
using ShopSphere.Application.Users.Login;
using ShopSphere.Application.Users.Refresh;
using ShopSphere.Application.Users.Register;

namespace ShopSphere.Api.Endpoints.Auth;

internal sealed class AuthEndpoints : IEndpoint
{
    public sealed record RegisterRequest(string Email, string FirstName, string LastName, string Password);

    public sealed record LoginRequest(string Email, string Password);

    public sealed record RefreshRequest(string RefreshToken);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("auth")
            .WithTags(Tags.Auth)
            .RequireRateLimiting(RateLimitingExtensions.AuthPolicy);

        group.MapPost("/register", async (
                RegisterRequest request,
                ICommandHandler<RegisterUserCommand, Guid> handler,
                CancellationToken cancellationToken) =>
            {
                var command = new RegisterUserCommand(request.Email, request.FirstName, request.LastName, request.Password);
                var result = await handler.Handle(command, cancellationToken);
                return result.Match(id => Results.Created("/api/v1/auth/me", new { id }));
            })
            .WithName("Register")
            .WithSummary("Registers a new customer account")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPost("/login", async (
                LoginRequest request,
                ICommandHandler<LoginUserCommand, TokenResponse> handler,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.Handle(new LoginUserCommand(request.Email, request.Password), cancellationToken);
                return result.Match(Results.Ok);
            })
            .WithName("Login")
            .WithSummary("Exchanges credentials for an access and refresh token")
            .Produces<TokenResponse>()
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        group.MapPost("/refresh", async (
                RefreshRequest request,
                ICommandHandler<RefreshTokenCommand, TokenResponse> handler,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.Handle(new RefreshTokenCommand(request.RefreshToken), cancellationToken);
                return result.Match(Results.Ok);
            })
            .WithName("RefreshToken")
            .WithSummary("Rotates a refresh token and issues a new access token")
            .Produces<TokenResponse>()
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        group.MapGet("/me", async (
                IQueryHandler<GetCurrentUserQuery, UserResponse> handler,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.Handle(new GetCurrentUserQuery(), cancellationToken);
                return result.Match(Results.Ok);
            })
            .RequireAuthorization()
            .WithName("GetCurrentUser")
            .WithSummary("Returns the authenticated user's profile")
            .Produces<UserResponse>();
    }
}
