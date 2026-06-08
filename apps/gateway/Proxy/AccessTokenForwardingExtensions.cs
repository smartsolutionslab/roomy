using Microsoft.AspNetCore.Authentication;
using Microsoft.Net.Http.Headers;
using Yarp.ReverseProxy.Transforms;
using Yarp.ReverseProxy.Transforms.Builder;

namespace SmartSolutionsLab.Roomy.Gateway.Proxy;

// The "token to services" half of the BFF pattern (ADR-0013): for every proxied request the
// gateway lifts the access token out of the server-side session and forwards it as a Bearer
// header to the downstream context API, which validates it as a resource server. The browser
// never sees the token — it only presented the session cookie. No downstream exists yet, so
// this wires the pattern ahead of the first context API rather than inventing an upstream.
public static class AccessTokenForwardingExtensions
{
    private const string AccessTokenName = "access_token";

    public static IReverseProxyBuilder AddAccessTokenForwarding(this IReverseProxyBuilder builder)
    {
        builder.AddTransforms(context =>
            context.AddRequestTransform(async transform =>
            {
                var accessToken = await transform.HttpContext
                    .GetTokenAsync(AccessTokenName)
                    .ConfigureAwait(false);

                if (string.IsNullOrEmpty(accessToken))
                    return;

                transform.ProxyRequest.Headers.Remove(HeaderNames.Authorization);
                transform.ProxyRequest.Headers.TryAddWithoutValidation(
                    HeaderNames.Authorization,
                    $"Bearer {accessToken}");
            }));

        return builder;
    }
}
