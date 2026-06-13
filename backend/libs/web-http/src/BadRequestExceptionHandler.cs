using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using SmartSolutionsLab.Roomy.SharedKernel.Results;

namespace SmartSolutionsLab.Roomy.Web.Http;

public sealed class BadRequestExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        if (exception is not BadRequestException badRequest) return false;

        httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
        await httpContext.Response.WriteAsJsonAsync(
            new ErrorResponse(badRequest.Error.Code, badRequest.Error.Message),
            cancellationToken);

        return true;
    }
}
