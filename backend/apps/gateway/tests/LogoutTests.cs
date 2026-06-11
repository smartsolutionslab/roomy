using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Shouldly;

namespace SmartSolutionsLab.Roomy.Gateway.Tests;

public sealed class LogoutTests(GatewayApplicationFactory factory) : IClassFixture<GatewayApplicationFactory>
{
    private HttpClient AuthenticatedClient()
    {
        var client = factory.CreateClient(
            new WebApplicationFactoryClientOptions { AllowAutoRedirect = false, HandleCookies = false });
        client.DefaultRequestHeaders.Add(GatewayTestAuthHandler.AuthenticatedHeader, "true");
        return client;
    }

    [Fact]
    public async Task Logout_ends_the_session_by_clearing_the_bff_cookie()
    {
        var response = await AuthenticatedClient()
            .PostAsync("/bff/logout", content: null, TestContext.Current.CancellationToken);

        response.Headers.TryGetValues("Set-Cookie", out var setCookies).ShouldBeTrue();
        setCookies.ShouldContain(cookie =>
            cookie.Contains("__Host-roomy.bff=", StringComparison.Ordinal)
            && cookie.Contains("expires=Thu, 01 Jan 1970", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Logout_redirects_to_keycloak_rp_initiated_end_session()
    {
        var response = await AuthenticatedClient()
            .PostAsync("/bff/logout", content: null, TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Redirect);
        response.Headers.Location.ShouldNotBeNull();
        response.Headers.Location.ToString().ShouldStartWith(GatewayApplicationFactory.EndSessionEndpoint);
    }
}
