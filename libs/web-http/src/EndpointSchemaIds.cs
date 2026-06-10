using System.Text.Json.Serialization.Metadata;
using Microsoft.AspNetCore.OpenApi;

namespace SmartSolutionsLab.Roomy.Web.Http;

public static class EndpointSchemaIds
{
    public static string? ForEndpointDto(JsonTypeInfo typeInfo)
    {
        var suffix = typeInfo.Type.Namespace switch
        {
            { } ns when ns.EndsWith(".Response.Page", StringComparison.Ordinal) => "Page",
            { } ns when ns.EndsWith(".Response", StringComparison.Ordinal) => "Response",
            { } ns when ns.EndsWith(".Request", StringComparison.Ordinal) => "Request",
            _ => null,
        };

        return suffix is null
            ? OpenApiOptions.CreateDefaultSchemaReferenceId(typeInfo)
            : typeInfo.Type.Name + suffix;
    }
}
