using System.Text.Json.Serialization.Metadata;
using Microsoft.AspNetCore.OpenApi;

namespace SmartSolutionsLab.Roomy.Web.Http;

// Keeps the emitted OpenAPI schema names stable (ADR-0036/0050) now that endpoint DTOs drop their
// folder-redundant suffix and are qualified by sub-namespace (Response.X / Request.X / Page.X). The wire
// schema id is reconstructed as <TypeName><Suffix> from the namespace tail, so the C# type
// `…Endpoints.Response.Employee` is still emitted as "EmployeeResponse" and `…Endpoints.Response.Page.Employee`
// as "EmployeePage" — no client drift, and the Response/Page name pairs never collide. Every other type
// (ProblemDetails, ErrorResponse, …) keeps the framework's default short-name id.
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
