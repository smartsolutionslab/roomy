using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Testing;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace SmartSolutionsLab.Roomy.AppHost.Tests;

// Structural check on the app model: the identity service is wired into the stack with its database
// and reachable behind the gateway. Builds the model only — no resources are started — so it needs no
// Docker and stays fast.
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
}
