using Microsoft.AspNetCore.Authentication;
using Microsoft.Net.Http.Headers;
using Yarp.ReverseProxy.Configuration;
using Yarp.ReverseProxy.Transforms;

namespace SmartSolutionsLab.Roomy.Gateway.Proxy;

public static class AccessTokenForwardingExtensions
{
    private const string AccessTokenName = "access_token";

    public const string ForwardAccessTokenMetadataKey = "ForwardAccessToken";

    public static IReverseProxyBuilder AddAccessTokenForwarding(this IReverseProxyBuilder builder)
    {
        builder.AddTransforms(context =>
        {
            if (!ForwardsAccessToken(context.Route)) return;

            context.AddRequestTransform(async transform =>
            {
                var accessToken = await transform.HttpContext.GetTokenAsync(AccessTokenName).ConfigureAwait(false);

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
