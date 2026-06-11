using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using SmartSolutionsLab.Roomy.SharedKernel.Guards;

namespace SmartSolutionsLab.Roomy.Gateway.Authentication;

public static class BffAuthenticationExtensions
{
    public const string CookieScheme = "RoomyBff";
    public const string OidcScheme = OpenIdConnectDefaults.AuthenticationScheme;

    // Keycloak emits realm roles under realm_access.roles; mapping them to ClaimTypes.Role
    // lets the gateway and downstream policies use the standard role machinery.
    private const string RealmAccessClaim = "realm_access";

    public static IServiceCollection AddBffAuthentication(this IServiceCollection services)
    {
        Ensure.That((IServiceCollection?)services).IsNotNull();

        services.AddOptions<KeycloakOidcOptions>()
            .BindConfiguration(KeycloakOidcOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddAuthentication(configure =>
            {
                configure.DefaultScheme = CookieScheme;
                configure.DefaultChallengeScheme = OidcScheme;
            })
            .AddCookie(CookieScheme, ConfigureCookie)
            .AddOpenIdConnect(OidcScheme, ConfigureOpenIdConnect);

        services.AddOptions<OpenIdConnectOptions>(OidcScheme)
            .Configure<IOptions<KeycloakOidcOptions>>(ApplyKeycloakOptions);

        services.AddHttpClient(BffTokenRefresher.HttpClientName);

        services.AddMemoryCache();
        services.AddSingleton<ITicketStore, MemoryTicketStore>();
        services.AddOptions<CookieAuthenticationOptions>(CookieScheme)
            .Configure<ITicketStore>((options, ticketStore) => options.SessionStore = ticketStore);

        services.AddAuthorization();

        return services;
    }

    private static void ConfigureCookie(CookieAuthenticationOptions options)
    {
        options.Cookie.Name = "__Host-roomy.bff";
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.SlidingExpiration = true;

        options.Events.OnValidatePrincipal = BffTokenRefresher.ValidateOrRefreshAsync;

        options.Events.OnRedirectToLogin = context => ReplyWithStatus(context, StatusCodes.Status401Unauthorized);
        options.Events.OnRedirectToAccessDenied = context => ReplyWithStatus(context, StatusCodes.Status403Forbidden);
    }

    private static void ConfigureOpenIdConnect(OpenIdConnectOptions options)
    {
        options.SignInScheme = CookieScheme;

        options.ResponseType = OpenIdConnectResponseType.Code;
        options.UsePkce = true;

        options.SaveTokens = true;
        options.GetClaimsFromUserInfoEndpoint = true;
        options.MapInboundClaims = false;

        options.Scope.Clear();
        options.Scope.Add(OpenIdConnectScope.OpenIdProfile);
        options.Scope.Add("email");
        options.Scope.Add(OpenIdConnectScope.OfflineAccess);

        options.TokenValidationParameters.NameClaimType = JwtRegisteredClaimNames.PreferredUsername;
        options.TokenValidationParameters.RoleClaimType = ClaimTypes.Role;

        options.Events.OnTokenValidated = FlattenRealmRoles;
    }

    private static void ApplyKeycloakOptions(OpenIdConnectOptions options, IOptions<KeycloakOidcOptions> keycloakOptions)
    {
        var keycloak = keycloakOptions.Value;

        options.Authority = BuildAuthority(keycloak);
        options.ClientId = keycloak.ClientId;
        options.ClientSecret = keycloak.ClientSecret;
        options.RequireHttpsMetadata = keycloak.RequireHttpsMetadata;
    }

    private static string BuildAuthority(KeycloakOidcOptions keycloak) =>
        $"{keycloak.Authority.TrimEnd('/')}/realms/{keycloak.Realm}";

    // Keycloak nests realm roles inside the realm_access JSON claim. Promote each to a flat
    // role claim so authorization policies and the whoami endpoint can read them directly.
    private static Task FlattenRealmRoles(TokenValidatedContext context)
    {
        if (context.Principal?.Identity is not ClaimsIdentity identity) return Task.CompletedTask;

        var realmAccess = identity.FindFirst(RealmAccessClaim);
        if (realmAccess is null) return Task.CompletedTask;

        foreach (var role in RealmRoleReader.ReadRoles(realmAccess.Value))
        {
            if (!identity.HasClaim(ClaimTypes.Role, role))
            {
                identity.AddClaim(new Claim(ClaimTypes.Role, role));
            }
        }

        return Task.CompletedTask;
    }

    private static Task ReplyWithStatus(Microsoft.AspNetCore.Authentication.RedirectContext<CookieAuthenticationOptions> context, int statusCode)
    {
        context.Response.StatusCode = statusCode;
        return Task.CompletedTask;
    }
}
