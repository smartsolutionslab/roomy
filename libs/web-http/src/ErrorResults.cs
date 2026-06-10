using Microsoft.AspNetCore.Http;
using SmartSolutionsLab.Roomy.SharedKernel.Results;

namespace SmartSolutionsLab.Roomy.Web.Http;

// The single domain-Error → HTTP-response mapping for every context API (ADR-0046). The error kind picks the
// status; the body is always an ErrorResponse(code, message). Request-shape validation (a malformed cursor,
// a missing body) is a 400 via ToBadRequest — it is not a domain rule, so it stays distinct from the kinds
// the use cases return.
public static class ErrorResults
{
    public static IResult ToHttpResult(this Error error)
    {
        var status = error.Type switch
        {
            ErrorType.Validation => StatusCodes.Status422UnprocessableEntity,
            ErrorType.NotFound => StatusCodes.Status404NotFound,
            ErrorType.Conflict => StatusCodes.Status409Conflict,
            ErrorType.Forbidden => StatusCodes.Status403Forbidden,
            ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
            _ => StatusCodes.Status500InternalServerError,
        };

        return Results.Json(new ErrorResponse(error.Code, error.Message), statusCode: status);
    }

    public static IResult ToBadRequest(this Error error) =>
        Results.Json(new ErrorResponse(error.Code, error.Message), statusCode: StatusCodes.Status400BadRequest);
}
