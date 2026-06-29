using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace SmartSolutionsLab.Roomy.Infrastructure.Authentication;

// Validates the BFF-forwarded Keycloak access token as a JWT bearer against the realm (ADR-0013). The
// audience is not validated — the gateway gates access and a Keycloak token's audience varies by client —
// but the issuer/realm must match. Keycloak nests realm roles under realm_access.roles; they are flattened
// to ClaimTypes.Role claims so administrator-only routes can authorize on RequireRole. Login/session stay the
// BFF's concern; this only validates the token and shapes claims. Shared by every context API host (ADR-0045).
public static class KeycloakJwtBearerExtensions
{
    public static IServiceCollection AddKeycloakJwtBearer(
        this IServiceCollection services,
        Uri keycloakBaseAddress,
        string realm,
        IHostEnvironment environment,
        IConfiguration configuration)
    {
        var requireHttpsMetadata = ResolveRequireHttpsMetadata(environment, configuration);

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.Authority = $"{keycloakBaseAddress.ToString().TrimEnd('/')}/realms/{realm}";
                options.RequireHttpsMetadata = requireHttpsMetadata;
                options.TokenValidationParameters.ValidateAudience = false;
                options.Events = new()
                {
                    OnTokenValidated = context =>
                    {
                        KeycloakRealmRoles.AddRoleClaims(context.Principal);
                        return Task.CompletedTask;
                    },
                };
            });
        services.AddAuthorization();

        return services;
    }

    // Secure by default: a non-Development resource server must fetch Keycloak signing-key metadata over
    // HTTPS (ADR-0013). Development relaxes it so local Keycloak over plain http works under Aspire. An
    // explicit Keycloak:RequireHttpsMetadata wins either way; a malformed value falls back to the
    // environment default rather than silently disabling the requirement.
    private static bool ResolveRequireHttpsMetadata(IHostEnvironment environment, IConfiguration configuration) =>
        bool.TryParse(configuration["Keycloak:RequireHttpsMetadata"], out var configured)
            ? configured
            : !environment.IsDevelopment();
}
