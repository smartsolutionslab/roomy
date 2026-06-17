using Microsoft.AspNetCore.Http;
using SmartSolutionsLab.Roomy.SharedKernel.Results;

namespace SmartSolutionsLab.Roomy.Web.Http;

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

        return Json(error, status);
    }

    public static IResult ToBadRequest(this Error error) =>
        Json(error, StatusCodes.Status400BadRequest);

    private static IResult Json(Error error, int statusCode) =>
        Results.Json(new ErrorResponse(error.Code, error.Message), statusCode: statusCode);
}
