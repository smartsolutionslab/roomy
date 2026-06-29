using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Shouldly;
using SmartSolutionsLab.Roomy.Infrastructure.Authentication;

namespace SmartSolutionsLab.Roomy.Identity.IntegrationTests;

public sealed class KeycloakJwtBearerTests
{
    [Theory]
    [InlineData("Production", null, true)]
    [InlineData("Staging", null, true)]
    [InlineData("Development", null, false)]
    [InlineData("Production", "false", false)]
    [InlineData("Development", "true", true)]
    [InlineData("Production", "not-a-bool", true)]
    [InlineData("Development", "not-a-bool", false)]
    public void Resolves_require_https_metadata_from_environment_and_override(
        string environmentName, string? configuredOverride, bool expected) =>
        RequireHttpsMetadata(environmentName, configuredOverride).ShouldBe(expected);

    private static bool RequireHttpsMetadata(string environmentName, string? configuredOverride)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configuredOverride is null
                ? []
                : new Dictionary<string, string?> { ["Keycloak:RequireHttpsMetadata"] = configuredOverride })
            .Build();

        var services = new ServiceCollection();
        // An https authority so resolving the options never trips JwtBearer's post-configure guard, which
        // throws when RequireHttpsMetadata is true against an http authority — exactly the protection this
        // gate restores. We assert the resolved flag, not that guard.
        services.AddKeycloakJwtBearer(
            new Uri("https://keycloak.localhost"), "roomy", new FakeEnvironment(environmentName), configuration);

        using var provider = services.BuildServiceProvider();
        return provider.GetRequiredService<IOptionsMonitor<JwtBearerOptions>>()
            .Get(JwtBearerDefaults.AuthenticationScheme)
            .RequireHttpsMetadata;
    }

    private sealed class FakeEnvironment(string environmentName) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = environmentName;
        public string ApplicationName { get; set; } = "tests";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
