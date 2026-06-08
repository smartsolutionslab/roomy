using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Shouldly;
using SmartSolutionsLab.Roomy.Gateway.Authentication;

namespace SmartSolutionsLab.Roomy.Gateway.Tests;

// The OIDC handler validates its options (which require ClientId/Authority) when it is first
// initialized for a request. The Keycloak settings must therefore be applied to the handler at
// configuration time, not lazily in a request event — otherwise validation throws on every
// request (ArgumentNullException for ClientId), which is what these tests guard against.
public sealed class BffAuthenticationOptionsTests
{
    [Fact]
    public void Applies_keycloak_client_settings_to_the_oidc_handler()
    {
        var options = ResolveOidcOptions();

        options.ClientId.ShouldBe("roomy-bff");
        options.ClientSecret.ShouldBe("test-secret");
        options.RequireHttpsMetadata.ShouldBeFalse();
    }

    [Fact]
    public void Builds_the_authority_from_the_keycloak_base_url_and_realm()
    {
        var options = ResolveOidcOptions();

        options.Authority.ShouldBe("http://localhost:8080/realms/roomy");
    }

    private static OpenIdConnectOptions ResolveOidcOptions()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["Authentication:Keycloak:Authority"] = "http://localhost:8080",
                    ["Authentication:Keycloak:Realm"] = "roomy",
                    ["Authentication:Keycloak:ClientId"] = "roomy-bff",
                    ["Authentication:Keycloak:ClientSecret"] = "test-secret",
                    ["Authentication:Keycloak:RequireHttpsMetadata"] = "false",
                })
            .Build();

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddLogging();
        services.AddDataProtection();
        services.AddBffAuthentication();

        using var provider = services.BuildServiceProvider();

        return provider
            .GetRequiredService<IOptionsMonitor<OpenIdConnectOptions>>()
            .Get(BffAuthenticationExtensions.OidcScheme);
    }
}
