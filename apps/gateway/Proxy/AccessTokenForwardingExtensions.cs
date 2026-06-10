using Microsoft.AspNetCore.Authentication;
using Microsoft.Net.Http.Headers;
using Yarp.ReverseProxy.Configuration;
using Yarp.ReverseProxy.Transforms;

namespace SmartSolutionsLab.Roomy.Gateway.Proxy;

// The "token to services" half of the BFF pattern (ADR-0013): for a proxied request to a
// downstream context API the gateway lifts the access token out of the server-side session and
// forwards it as a Bearer header; that API validates it as a resource server. The browser never
// sees the token — it only presented the session cookie.
//
// Forwarding is opt-in per route via `Metadata: { ForwardAccessToken: "true" }` so it reaches
// only resource servers. The single-origin SPA proxy route (ADR-0030) must never receive the
// token — its destination is a static asset server, not a resource server.
public static class AccessTokenForwardingExtensions
{
    private const string AccessTokenName = "access_token";

    // A route targeting a resource server sets this metadata key to "true" to receive the token.
    public const string ForwardAccessTokenMetadataKey = "ForwardAccessToken";

    public static IReverseProxyBuilder AddAccessTokenForwarding(this IReverseProxyBuilder builder)
    {
        builder.AddTransforms(context =>
        {
            if (!ForwardsAccessToken(context.Route)) return;

            context.AddRequestTransform(async transform =>
            {
                var accessToken = await transform.HttpContext
                    .GetTokenAsync(AccessTokenName)
                    .ConfigureAwait(false);

                if (string.IsNullOrEmpty(accessToken)) return;

                transform.ProxyRequest.Headers.Remove(HeaderNames.Authorization);
                transform.ProxyRequest.Headers.TryAddWithoutValidation(HeaderNames.Authorization, $"Bearer {accessToken}");
            });
        });

        return builder;
    }

    internal static bool ForwardsAccessToken(RouteConfig route) =>
        route.Metadata is { } metadata
        && metadata.TryGetValue(ForwardAccessTokenMetadataKey, out var value)
        && string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);
}
