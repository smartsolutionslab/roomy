using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Shouldly;
using SmartSolutionsLab.Roomy.Organization.Api;

namespace SmartSolutionsLab.Roomy.Organization.IntegrationTests;

public sealed class OpenApiDocumentTests : IDisposable
{
    private readonly WebApplicationFactory<OrganizationApiHost> app;

    public OpenApiDocumentTests()
    {
        app = new WebApplicationFactory<OrganizationApiHost>().WithWebHostBuilder(webHost =>
        {
            webHost.UseSetting("ConnectionStrings:organization", "Host=localhost;Database=organization;Username=ci;Password=ci");
            webHost.UseSetting("ConnectionStrings:rabbitmq", "amqp://guest:guest@localhost:5672");
            webHost.UseSetting("Keycloak:BaseAddress", "http://keycloak.localhost");
            webHost.UseSetting("Keycloak:Realm", "roomy");
            webHost.UseSetting("Company:Name", "Roomy Test Company");
            webHost.UseSetting("DefaultAdmin:Email", "default-admin@roomy.test");
            webHost.UseSetting("DefaultAdmin:DisplayName", "Default Admin");
            webHost.UseSetting("DefaultAdmin:InitialPassword", "default-admin-password");

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
    public async Task Types_a_bad_request_as_error_response()
    {
        using var document = await GetDocumentAsync();

        document.RootElement.GetProperty("paths").GetProperty("/offices").GetProperty("post")
            .GetProperty("responses").GetProperty("400")
            .GetProperty("content").GetProperty("application/json").GetProperty("schema").GetProperty("$ref").GetString()
            .ShouldBe("#/components/schemas/ErrorResponse");
    }

    private async Task<JsonDocument> GetDocumentAsync() =>
        JsonDocument.Parse(await app.CreateClient()
            .GetStringAsync("/openapi/v1.json", TestContext.Current.CancellationToken));

    public void Dispose() => app.Dispose();
}
