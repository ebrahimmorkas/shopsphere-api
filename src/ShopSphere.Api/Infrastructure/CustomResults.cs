using ShopSphere.Domain.Abstractions;

namespace ShopSphere.Api.Infrastructure;

/// <summary>
/// Maps failed <see cref="Result"/> instances to RFC 9457 problem details responses.
/// </summary>
public static class CustomResults
{
    public static IResult Problem(Result result)
    {
        if (result.IsSuccess)
        {
            throw new InvalidOperationException("Cannot create a problem response from a successful result.");
        }

        var error = result.Error;

        return Results.Problem(
            title: GetTitle(error),
            detail: error.Description,
            type: GetType(error.Type),
            statusCode: GetStatusCode(error.Type),
            extensions: GetExtensions(error));
    }

    private static string GetTitle(Error error) => error.Type switch
    {
        ErrorType.Validation or ErrorType.NotFound or ErrorType.Conflict => error.Code,
        ErrorType.Unauthorized => "Unauthorized",
        ErrorType.Forbidden => "Forbidden",
        _ => "Server failure"
    };

    private static string GetType(ErrorType errorType) => errorType switch
    {
        ErrorType.Validation => "https://tools.ietf.org/html/rfc9110#section-15.5.1",
        ErrorType.Unauthorized => "https://tools.ietf.org/html/rfc9110#section-15.5.2",
        ErrorType.Forbidden => "https://tools.ietf.org/html/rfc9110#section-15.5.4",
        ErrorType.NotFound => "https://tools.ietf.org/html/rfc9110#section-15.5.5",
        ErrorType.Conflict => "https://tools.ietf.org/html/rfc9110#section-15.5.10",
        _ => "https://tools.ietf.org/html/rfc9110#section-15.6.1"
    };

    private static int GetStatusCode(ErrorType errorType) => errorType switch
    {
        ErrorType.Validation => StatusCodes.Status400BadRequest,
        ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
        ErrorType.Forbidden => StatusCodes.Status403Forbidden,
        ErrorType.NotFound => StatusCodes.Status404NotFound,
        ErrorType.Conflict => StatusCodes.Status409Conflict,
        _ => StatusCodes.Status500InternalServerError
    };

    private static Dictionary<string, object?>? GetExtensions(Error error)
    {
        if (error is not ValidationError validationError)
        {
            return null;
        }

        var errors = validationError.Errors
            .GroupBy(e => e.Code)
            .ToDictionary(g => g.Key, g => g.Select(e => e.Description).ToArray());

        return new Dictionary<string, object?> { ["errors"] = errors };
    }
}
