using Microsoft.AspNetCore.Http;
using SmartSolutionsLab.Roomy.SharedKernel.Results;

namespace SmartSolutionsLab.Roomy.Web.Http;

public static class ResultResults
{
    public static IResult ToOk<TValue, TResponse>(
        this Result<TValue> result,
        Func<TValue, TResponse> onSuccess,
        Func<Error, IResult>? onError = null) =>
        result.Match(value => Results.Ok(onSuccess(value)), onError ?? ErrorResults.ToHttpResult);

    public static IResult ToNoContent(
        this Result result,
        Func<Error, IResult>? onError = null) =>
        result.Match(Results.NoContent, onError ?? ErrorResults.ToHttpResult);

    public static IResult ToCreated<TValue, TResponse>(
        this Result<TValue> result,
        Func<TValue, string> location,
        Func<TValue, TResponse> onSuccess,
        Func<Error, IResult>? onError = null) =>
        result.Match(
            value => Results.Created(location(value), onSuccess(value)),
            onError ?? ErrorResults.ToHttpResult);
}
