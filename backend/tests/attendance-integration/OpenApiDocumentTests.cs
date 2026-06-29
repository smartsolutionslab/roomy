using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Shouldly;
using SmartSolutionsLab.Roomy.Attendance.Api;

namespace SmartSolutionsLab.Roomy.Attendance.IntegrationTests;

public sealed class OpenApiDocumentTests : IDisposable
{
    private readonly WebApplicationFactory<AttendanceApiHost> app;

    public OpenApiDocumentTests()
    {
        app = new WebApplicationFactory<AttendanceApiHost>().WithWebHostBuilder(webHost =>
        {
            webHost.UseSetting("ConnectionStrings:attendance", "Host=localhost;Database=attendance;Username=ci;Password=ci");
            webHost.UseSetting("ConnectionStrings:rabbitmq", "amqp://guest:guest@localhost:5672");
            webHost.UseSetting("Keycloak:BaseAddress", "http://keycloak.localhost");
            webHost.UseSetting("Keycloak:Realm", "roomy");
            webHost.UseSetting("Attendance:CompanyId", "00000000-0000-0000-0000-000000000001");
            webHost.UseSetting("Messaging:Enabled", "false");

            webHost.ConfigureTestServices(services => services.RemoveAll<IHostedService>());
        });
    }

    [Fact]
    public async Task Declares_no_problem_details_error_bodies()
    {
        var json = await app.CreateClient().GetStringAsync("/openapi/v1.json", TestContext.Current.CancellationToken);

        json.ShouldNotContain("application/problem+json");
    }

    [Fact]
    public async Task Types_a_handler_driven_forbidden_as_error_response()
    {
        using var document = await GetDocumentAsync();

        ErrorSchemaReference(document, "/reservations", "post", "403")
            .ShouldBe("#/components/schemas/ErrorResponse");
    }

    [Fact]
    public async Task Types_a_bad_request_as_error_response()
    {
        using var document = await GetDocumentAsync();

        ErrorSchemaReference(document, "/occupancy", "get", "400")
            .ShouldBe("#/components/schemas/ErrorResponse");
    }

    [Fact]
    public async Task Types_a_policy_only_forbidden_as_empty()
    {
        using var document = await GetDocumentAsync();

        Response(document, "/reservations/employees", "get", "403")
            .TryGetProperty("content", out _).ShouldBeFalse();
    }

    private static string? ErrorSchemaReference(JsonDocument document, string path, string method, string status) =>
        Response(document, path, method, status)
            .GetProperty("content").GetProperty("application/json").GetProperty("schema").GetProperty("$ref").GetString();

    private static JsonElement Response(JsonDocument document, string path, string method, string status) =>
        document.RootElement.GetProperty("paths").GetProperty(path).GetProperty(method)
            .GetProperty("responses").GetProperty(status);

    private async Task<JsonDocument> GetDocumentAsync() =>
        JsonDocument.Parse(await app.CreateClient()
            .GetStringAsync("/openapi/v1.json", TestContext.Current.CancellationToken));

    public void Dispose() => app.Dispose();
}
