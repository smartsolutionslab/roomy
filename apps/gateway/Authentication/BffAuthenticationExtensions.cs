using System.Globalization;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using SmartSolutionsLab.Roomy.SharedKernel.Guards;

namespace SmartSolutionsLab.Roomy.Gateway.Authentication;

// Wires the BFF security pattern (ADR-0013): a session cookie for the browser and a
// server-side OpenID Connect auth-code flow (with PKCE) against Keycloak. Tokens are kept
// in the encrypted server-side session (SaveTokens) and never handed to the SPA.
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

        // Apply the Keycloak client settings to the OIDC handler at configuration time. The
        // handler validates its options (ClientId and Authority are required) the first time it
        // is initialized for a request, so they must be set before validation runs — not lazily
        // in a request event, which fires too late and makes every request fail validation.
        services.AddOptions<OpenIdConnectOptions>(OidcScheme)
            .Configure<IOptions<KeycloakOidcOptions>>(ApplyKeycloakOptions);

        services.AddAuthorization();

        return services;
    }

    private static void ConfigureCookie(CookieAuthenticationOptions options)
    {
        // The only artefact the browser ever holds: an HTTP-only, Secure, SameSite session
        // cookie. No access or refresh token is exposed to client-side script (ADR-0013).
        options.Cookie.Name = "__Host-roomy.bff";
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.SlidingExpiration = true;

        // The SPA owns navigation: API calls answer 401/403 rather than redirecting to the
        // identity provider, so an XHR never follows a login redirect.
        options.Events.OnRedirectToLogin = context => ReplyWithStatus(context, StatusCodes.Status401Unauthorized);
        options.Events.OnRedirectToAccessDenied = context => ReplyWithStatus(context, StatusCodes.Status403Forbidden);
    }

    private static void ConfigureOpenIdConnect(OpenIdConnectOptions options)
    {
        options.SignInScheme = CookieScheme;

        options.ResponseType = OpenIdConnectResponseType.Code;
        options.UsePkce = true;

        // Keep the tokens server-side in the session so the BFF can attach the access token
        // to downstream context-API calls, while the browser only ever sees the cookie.
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

    private static void ApplyKeycloakOptions(
        OpenIdConnectOptions options,
        IOptions<KeycloakOidcOptions> keycloakOptions)
    {
        var keycloak = keycloakOptions.Value;

        options.Authority = BuildAuthority(keycloak);
        options.ClientId = keycloak.ClientId;
        options.ClientSecret = keycloak.ClientSecret;
        options.RequireHttpsMetadata = keycloak.RequireHttpsMetadata;
    }

    private static string BuildAuthority(KeycloakOidcOptions keycloak) =>
        string.Create(
            CultureInfo.InvariantCulture,
            $"{keycloak.Authority.TrimEnd('/')}/realms/{keycloak.Realm}");

    // Keycloak nests realm roles inside the realm_access JSON claim. Promote each to a flat
    // role claim so authorization policies and the whoami endpoint can read them directly.
    private static Task FlattenRealmRoles(TokenValidatedContext context)
    {
        if (context.Principal?.Identity is not ClaimsIdentity identity)
            return Task.CompletedTask;

        var realmAccess = identity.FindFirst(RealmAccessClaim);
        if (realmAccess is null)
            return Task.CompletedTask;

        foreach (var role in RealmRoleReader.ReadRoles(realmAccess.Value))
        {
            if (!identity.HasClaim(ClaimTypes.Role, role))
                identity.AddClaim(new Claim(ClaimTypes.Role, role));
        }

        return Task.CompletedTask;
    }

    private static Task ReplyWithStatus(
        Microsoft.AspNetCore.Authentication.RedirectContext<CookieAuthenticationOptions> context,
        int statusCode)
    {
        context.Response.StatusCode = statusCode;
        return Task.CompletedTask;
    }
}
