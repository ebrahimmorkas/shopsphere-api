using ShopSphere.Api.Infrastructure;
using ShopSphere.Domain.Abstractions;

namespace ShopSphere.Api.Extensions;

internal static class ResultExtensions
{
    public static IResult Match(this Result result, Func<IResult> onSuccess) =>
        result.IsSuccess ? onSuccess() : CustomResults.Problem(result);

    public static IResult Match<TValue>(this Result<TValue> result, Func<TValue, IResult> onSuccess) =>
        result.IsSuccess ? onSuccess(result.Value) : CustomResults.Problem(result);
}
