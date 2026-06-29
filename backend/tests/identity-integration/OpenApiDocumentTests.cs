using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Shouldly;
using SmartSolutionsLab.Roomy.Identity.Api;

namespace SmartSolutionsLab.Roomy.Identity.IntegrationTests;

public sealed class OpenApiDocumentTests : IDisposable
{
    private readonly WebApplicationFactory<IdentityApiHost> app;

    public OpenApiDocumentTests()
    {
        app = new WebApplicationFactory<IdentityApiHost>().WithWebHostBuilder(webHost =>
        {
            webHost.UseSetting("ConnectionStrings:identity", "Host=localhost;Database=identity;Username=ci;Password=ci");
            webHost.UseSetting("ConnectionStrings:rabbitmq", "amqp://guest:guest@localhost:5672");
            webHost.UseSetting("Keycloak:BaseAddress", "http://keycloak.localhost");
            webHost.UseSetting("Keycloak:AdminUsername", "admin");
            webHost.UseSetting("Keycloak:AdminPassword", "admin");
            webHost.UseSetting("DefaultAdmin:Email", "default-admin@roomy.test");
            webHost.UseSetting("DefaultAdmin:DisplayName", "Default Admin");
            webHost.UseSetting("DefaultAdmin:InitialPassword", "default-admin-password");

            webHost.ConfigureTestServices(services => services.RemoveAll<IHostedService>());
        });
    }

    [Fact]
    public async Task Publishes_the_account_me_contract_with_typed_responses()
    {
        using var document = await GetDocumentAsync();

        var operation = document.RootElement
            .GetProperty("paths")
            .GetProperty("/account/me")
            .GetProperty("get");

        var responses = operation.GetProperty("responses");
        responses.TryGetProperty("200", out _).ShouldBeTrue();
        responses.TryGetProperty("401", out _).ShouldBeTrue();
        responses.TryGetProperty("404", out _).ShouldBeTrue();

        var schemaReference = responses.GetProperty("200")
            .GetProperty("content").GetProperty("application/json")
            .GetProperty("schema").GetProperty("$ref").GetString();
        schemaReference.ShouldBe("#/components/schemas/AccountResponse");
    }

    [Fact]
    public async Task Names_the_account_operation_for_a_stable_generated_method()
    {
        using var document = await GetDocumentAsync();

        var operationId = document.RootElement
            .GetProperty("paths").GetProperty("/account/me").GetProperty("get")
            .GetProperty("operationId").GetString();

        operationId.ShouldBe("GetCurrentAccount");
    }

    [Fact]
    public async Task Declares_no_problem_details_error_bodies()
    {
        var json = await app.CreateClient().GetStringAsync("/openapi/v1.json", TestContext.Current.CancellationToken);

        json.ShouldNotContain("application/problem+json");
    }

    [Fact]
    public async Task Types_the_unauthorized_account_response_as_empty()
    {
        using var document = await GetDocumentAsync();

        var unauthorized = document.RootElement.GetProperty("paths").GetProperty("/account/me").GetProperty("get")
            .GetProperty("responses").GetProperty("401");
        unauthorized.TryGetProperty("content", out _).ShouldBeFalse();
    }

    [Fact]
    public async Task Types_the_admin_users_bad_request_as_error_response()
    {
        using var document = await GetDocumentAsync();

        var badRequest = document.RootElement.GetProperty("paths").GetProperty("/admin/users").GetProperty("get")
            .GetProperty("responses").GetProperty("400");
        badRequest.GetProperty("content").GetProperty("application/json").GetProperty("schema").GetProperty("$ref").GetString()
            .ShouldBe("#/components/schemas/ErrorResponse");
    }

    private async Task<JsonDocument> GetDocumentAsync() =>
        JsonDocument.Parse(await app.CreateClient()
            .GetStringAsync("/openapi/v1.json", TestContext.Current.CancellationToken));

    public void Dispose() => app.Dispose();
}
