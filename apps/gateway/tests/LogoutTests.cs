using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Shouldly;

namespace SmartSolutionsLab.Roomy.Gateway.Tests;

// Pins the BFF logout contract (FR-011, spec scenario 10): logging out ends the session by clearing
// the BFF cookie and triggers Keycloak RP-initiated end-session, so further actions require logging in
// again. The cookie scheme cleared here must match the one AddBffAuthentication registers — signing out
// the framework-default "Cookies" scheme would leave the real session cookie in place.
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
