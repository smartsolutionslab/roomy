using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using SmartSolutionsLab.Roomy.SharedKernel.Results;

namespace SmartSolutionsLab.Roomy.Web.Http;

public sealed class BadRequestExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var body = exception switch
        {
            BadRequestException badRequest => new ErrorResponse(badRequest.Error.Code, badRequest.Error.Message),
            ArgumentException argument => new ErrorResponse("bad_request", argument.Message),
            _ => null,
        };

        if (body is null) return false;

        httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
        await httpContext.Response.WriteAsJsonAsync(body, cancellationToken);

        return true;
    }
}
