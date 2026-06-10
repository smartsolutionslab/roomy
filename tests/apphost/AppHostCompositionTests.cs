using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Testing;
using Microsoft.Extensions.DependencyInjection;
using Scalar.Aspire;
using Shouldly;

namespace SmartSolutionsLab.Roomy.AppHost.Tests;

public sealed class AppHostCompositionTests
{
    [Fact]
    public async Task Composes_the_identity_service_with_its_database_behind_the_gateway()
    {
        var builder = await DistributedApplicationTestingBuilder.CreateAsync<Projects.Roomy_AppHost>(
            TestContext.Current.CancellationToken);
        await using var application = await builder.BuildAsync(TestContext.Current.CancellationToken);

        var model = application.Services.GetRequiredService<DistributedApplicationModel>();
        var resourceNames = model.Resources.Select(resource => resource.Name).ToList();

        resourceNames.ShouldContain("identity-api");
        resourceNames.ShouldContain("identity");
        resourceNames.ShouldContain("gateway");
        resourceNames.ShouldContain("keycloak");
        resourceNames.ShouldContain("postgres");
        resourceNames.ShouldContain("rabbitmq");
    }

    [Fact]
    public async Task Runs_the_migration_runner_to_completion_before_the_identity_service_starts()
    {
        var builder = await DistributedApplicationTestingBuilder.CreateAsync<Projects.Roomy_AppHost>(
            TestContext.Current.CancellationToken);
        await using var application = await builder.BuildAsync(TestContext.Current.CancellationToken);

        var model = application.Services.GetRequiredService<DistributedApplicationModel>();

        model.Resources.Select(resource => resource.Name).ShouldContain("db-migrator");

        var identityApi = model.Resources.Single(resource => resource.Name == "identity-api");
        identityApi.Annotations.OfType<WaitAnnotation>().ShouldContain(
            wait => wait.Resource.Name == "db-migrator" && wait.WaitType == WaitType.WaitForCompletion);
    }

    [Fact]
    public async Task Composes_the_organization_service_with_its_database_gated_on_the_migration_runner()
    {
        var builder = await DistributedApplicationTestingBuilder.CreateAsync<Projects.Roomy_AppHost>(
            TestContext.Current.CancellationToken);
        await using var application = await builder.BuildAsync(TestContext.Current.CancellationToken);

        var model = application.Services.GetRequiredService<DistributedApplicationModel>();
        var resourceNames = model.Resources.Select(resource => resource.Name).ToList();

        resourceNames.ShouldContain("organization-api");
        resourceNames.ShouldContain("organization");

        var organizationApi = model.Resources.Single(resource => resource.Name == "organization-api");
        organizationApi.Annotations.OfType<WaitAnnotation>().ShouldContain(
            wait => wait.Resource.Name == "db-migrator" && wait.WaitType == WaitType.WaitForCompletion);
    }

    [Fact]
    public async Task Composes_a_scalar_reference_and_an_openapi_link_for_each_context_api()
    {
        var builder = await DistributedApplicationTestingBuilder.CreateAsync<Projects.Roomy_AppHost>(
            TestContext.Current.CancellationToken);
        await using var application = await builder.BuildAsync(TestContext.Current.CancellationToken);

        var model = application.Services.GetRequiredService<DistributedApplicationModel>();

        model.Resources.OfType<ScalarResource>().ShouldHaveSingleItem();

        foreach (var apiName in new[] { "identity-api", "organization-api", "attendance-api" })
        {
            var api = model.Resources.Single(resource => resource.Name == apiName);
            api.Annotations.OfType<ResourceUrlsCallbackAnnotation>().ShouldNotBeEmpty();
        }
    }

    [Fact]
    public async Task Each_context_api_has_its_own_http_endpoint_so_they_do_not_collide_on_the_default_port()
    {
        var builder = await DistributedApplicationTestingBuilder.CreateAsync<Projects.Roomy_AppHost>(
            TestContext.Current.CancellationToken);
        await using var application = await builder.BuildAsync(TestContext.Current.CancellationToken);

        var model = application.Services.GetRequiredService<DistributedApplicationModel>();

        foreach (var apiName in new[] { "identity-api", "organization-api", "attendance-api" })
        {
            var api = model.Resources.Single(resource => resource.Name == apiName);
            api.Annotations.OfType<EndpointAnnotation>()
                .ShouldContain(endpoint => endpoint.Name == "http");
        }
    }
}
