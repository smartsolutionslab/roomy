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

var app = builder.Build();

app.MapDefaultEndpoints();

app.UseAuthentication();
app.UseAuthorization();

app.MapBffEndpoints();
app.MapReverseProxy();

app.Run();
