using SmartSolutionsLab.Roomy.SharedKernel.Results;

namespace SmartSolutionsLab.Roomy.Attendance.Api.Endpoints;

// Maps a domain Error to its HTTP response (contract: attendance-api.md). The body carries only the
// domain error code and a human message — no domain detail leaks beyond that.
internal static class ErrorResults
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
}

internal sealed record ErrorResponse(string Code, string Message);
