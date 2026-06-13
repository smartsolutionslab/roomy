using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;

namespace SmartSolutionsLab.Roomy.Web.Http;

public sealed class ArgumentExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        if (exception is not ArgumentException argument) return false;

        httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
        await httpContext.Response.WriteAsJsonAsync(new ErrorResponse("bad_request", argument.Message), cancellationToken);

        return true;
    }
}
