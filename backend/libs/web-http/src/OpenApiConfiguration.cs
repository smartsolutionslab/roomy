using Microsoft.Extensions.Configuration;

namespace SmartSolutionsLab.Roomy.Web.Http;

public static class OpenApiConfiguration
{
    public static bool IsEmittingOpenApiDocument(this IConfiguration configuration) =>
        configuration.GetValue<bool>("OpenApi:EmitDocument");
}
