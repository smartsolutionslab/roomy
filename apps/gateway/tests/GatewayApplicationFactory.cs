using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using SmartSolutionsLab.Roomy.Gateway.Authentication;

namespace SmartSolutionsLab.Roomy.Gateway.Tests;

// Boots the gateway in-process for the BFF endpoint tests. Keycloak is never contacted: the required
// OIDC settings are supplied so options validation passes, the discovery document is provided
// statically (so RP-initiated end-session builds its redirect without a metadata fetch), and a
// header-driven test scheme stands in for an authenticated session. The live OIDC round-trip against a
// real Keycloak is the deferred Testcontainers e2e (#73).
public sealed class GatewayApplicationFactory : WebApplicationFactory<RoomyGatewayHost>
{
    public const string EndSessionEndpoint =
        "https://keycloak.test/realms/roomy/protocol/openid-connect/logout";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("Authentication:Keycloak:Authority", "https://keycloak.test");
        builder.UseSetting("Authentication:Keycloak:Realm", "roomy");
        builder.UseSetting("Authentication:Keycloak:ClientId", "roomy-bff");
        builder.UseSetting("Authentication:Keycloak:ClientSecret", "test-secret");
        builder.UseSetting("Authentication:Keycloak:RequireHttpsMetadata", "false");

        builder.ConfigureTestServices(services =>
        {
            // Authenticate through the test scheme so RequireAuthorization is satisfied without a live
            // session, while leaving the real BFF cookie + OIDC schemes registered so logout still
            // clears the session cookie and targets the OIDC sign-out handler.
            services.AddAuthentication(GatewayTestAuthHandler.SchemeName)
                .AddScheme<AuthenticationSchemeOptions, GatewayTestAuthHandler>(
                    GatewayTestAuthHandler.SchemeName, _ => { });

            // Provide the OIDC discovery document statically so end-session builds its redirect without
            // reaching Keycloak's metadata endpoint.
            services.Configure<OpenIdConnectOptions>(BffAuthenticationExtensions.OidcScheme, options =>
                options.Configuration = new OpenIdConnectConfiguration
                {
                    Issuer = "https://keycloak.test/realms/roomy",
                    AuthorizationEndpoint = "https://keycloak.test/realms/roomy/protocol/openid-connect/auth",
                    TokenEndpoint = "https://keycloak.test/realms/roomy/protocol/openid-connect/token",
                    EndSessionEndpoint = EndSessionEndpoint,
                });
        });
    }
}
