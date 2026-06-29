using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace SmartSolutionsLab.Roomy.Web.Http;

public static class ProducesErrorExtensions
{
    public static RouteHandlerBuilder ProducesError(this RouteHandlerBuilder builder, int statusCode) =>
        builder.Produces<ErrorResponse>(statusCode);
}
