using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using SmartSolutionsLab.Roomy.Gateway.Authentication;

namespace SmartSolutionsLab.Roomy.Gateway.Tests;

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
            services.AddAuthentication(GatewayTestAuthHandler.SchemeName)
                .AddScheme<AuthenticationSchemeOptions, GatewayTestAuthHandler>(
                    GatewayTestAuthHandler.SchemeName, _ => { });

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
