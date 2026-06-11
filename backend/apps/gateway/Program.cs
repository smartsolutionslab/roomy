using SmartSolutionsLab.Roomy.Gateway.Authentication;
using SmartSolutionsLab.Roomy.Gateway.Bff;
using SmartSolutionsLab.Roomy.Gateway.Proxy;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddBffAuthentication();

builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"))
    .AddServiceDiscoveryDestinationResolver()
    .AddAccessTokenForwarding();

var httpsRedirectPort = builder.Configuration.GetValue<int?>("HttpsRedirection:HttpsPort");
if (httpsRedirectPort is not null)
{
    builder.Services.AddHttpsRedirection(options => options.HttpsPort = httpsRedirectPort.Value);
}

var app = builder.Build();

app.MapDefaultEndpoints();

// The BFF session cookie is __Host-prefixed and the OIDC form_post correlation cookie is SameSite=None;
// both require Secure, so the login flow only completes over HTTPS. When an external HTTPS port is
// configured, bounce plain-http requests to it — behind the Aspire/DCP proxy Kestrel can't infer the
// port itself. Left unset (tests, TLS-terminating ingress) the app does no redirect of its own.
if (httpsRedirectPort is not null)
{
    app.UseHttpsRedirection();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapBffEndpoints();
app.MapReverseProxy();

app.Run();
