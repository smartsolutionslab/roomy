using JasperFx;
using JasperFx.CommandLine;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Shouldly;
using SmartSolutionsLab.Roomy.Web.Http;

namespace SmartSolutionsLab.Roomy.Web.Http.Tests;

public class RoomyApiHostExtensionsTests
{
    private static readonly Uri keycloak = new("https://keycloak.localhost");

    [Fact]
    public void AddRoomyApiDefaults_registers_the_shared_bootstrap_services()
    {
        var builder = WebApplication.CreateBuilder();

        builder.AddRoomyApiDefaults(keycloak, "roomy");

        builder.Services.ShouldContain(descriptor => descriptor.ServiceType == typeof(IExceptionHandler));
        builder.Services.ShouldContain(descriptor => descriptor.ServiceType == typeof(IConfigureOptions<JwtBearerOptions>));
        builder.Services.ShouldContain(descriptor => descriptor.ServiceType == typeof(IAuthorizationService));
    }

    [Fact]
    public void AddRoomyApiDefaults_applies_the_emit_toggle_only_when_emitting()
    {
        JasperFxEnvironment.AutoStartHost = false;

        var notEmitting = WebApplication.CreateBuilder();
        notEmitting.AddRoomyApiDefaults(keycloak, "roomy");
        JasperFxEnvironment.AutoStartHost.ShouldBeFalse();

        var emitting = WebApplication.CreateBuilder();
        emitting.Configuration["OpenApi:EmitDocument"] = "true";
        emitting.AddRoomyApiDefaults(keycloak, "roomy");
        JasperFxEnvironment.AutoStartHost.ShouldBeTrue();

        JasperFxEnvironment.AutoStartHost = false;
    }
}
