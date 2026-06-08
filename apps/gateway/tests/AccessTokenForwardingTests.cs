using Shouldly;
using SmartSolutionsLab.Roomy.Gateway.Proxy;
using Yarp.ReverseProxy.Configuration;

namespace SmartSolutionsLab.Roomy.Gateway.Tests;

// The access token must only be forwarded to resource servers that opt in. In particular the
// single-origin SPA proxy route (ADR-0030) must never receive it (ADR-0013).
public sealed class AccessTokenForwardingTests
{
    [Fact]
    public void Does_not_forward_to_a_route_without_the_opt_in_metadata()
    {
        var spaRoute = new RouteConfig { RouteId = "spa", ClusterId = "web-dev-server" };

        AccessTokenForwardingExtensions.ForwardsAccessToken(spaRoute).ShouldBeFalse();
    }

    [Fact]
    public void Forwards_to_a_route_that_opts_in()
    {
        var apiRoute = new RouteConfig
        {
            RouteId = "attendance-api",
            Metadata = new Dictionary<string, string>
            {
                [AccessTokenForwardingExtensions.ForwardAccessTokenMetadataKey] = "true",
            },
        };

        AccessTokenForwardingExtensions.ForwardsAccessToken(apiRoute).ShouldBeTrue();
    }

    [Fact]
    public void Does_not_forward_when_the_metadata_disables_it()
    {
        var route = new RouteConfig
        {
            RouteId = "public",
            Metadata = new Dictionary<string, string>
            {
                [AccessTokenForwardingExtensions.ForwardAccessTokenMetadataKey] = "false",
            },
        };

        AccessTokenForwardingExtensions.ForwardsAccessToken(route).ShouldBeFalse();
    }
}
