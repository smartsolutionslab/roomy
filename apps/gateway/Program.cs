using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SmartSolutionsLab.Roomy.Gateway.Authentication;
using SmartSolutionsLab.Roomy.Gateway.Bff;
using SmartSolutionsLab.Roomy.Gateway.Proxy;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddBffAuthentication();

// YARP makes the gateway the single public entry point (ADR-0006/0018). Routes and clusters
// come from the "ReverseProxy" configuration section; context APIs are added there as they
// come online (#001+). Every proxied call carries the session's access token downstream.
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"))
    .AddAccessTokenForwarding();

var app = builder.Build();

app.MapDefaultEndpoints();

app.UseAuthentication();
app.UseAuthorization();

app.MapBffEndpoints();
app.MapReverseProxy();

app.Run();
