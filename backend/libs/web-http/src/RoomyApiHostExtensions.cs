using JasperFx;
using JasperFx.CommandLine;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SmartSolutionsLab.Roomy.Infrastructure.Authentication;

namespace SmartSolutionsLab.Roomy.Web.Http;

public static class RoomyApiHostExtensions
{
    public static WebApplicationBuilder AddRoomyApiDefaults(this WebApplicationBuilder builder, Uri keycloakBaseAddress, string realm)
    {
        builder.AddServiceDefaults();

        builder.Services.AddOpenApi(options => options.CreateSchemaReferenceId = EndpointSchemaIds.ForEndpointDto);
        builder.Services.AddRoomyExceptionHandling();
        builder.Services.AddKeycloakJwtBearer(keycloakBaseAddress, realm, builder.Environment, builder.Configuration);

        // Emitting the OpenAPI spec (ADR-0036) runs the host through `getdocument`. AutoStartHost lets that
        // HostFactoryResolver-based tool obtain the built service provider instead of the JasperFx dispatcher
        // disposing it first; it is scoped to the emit so the Wolverine `codegen write` step and normal
        // startup are unaffected. The host keeps gating its messaging runtime on the same emit flag.
        if (builder.Configuration.IsEmittingOpenApiDocument())
        {
            JasperFxEnvironment.AutoStartHost = true;
        }

        return builder;
    }

    public static Task<int> UseRoomyApiPipeline(this WebApplication app, string[] args)
    {
        app.MapDefaultEndpoints();

        app.UseExceptionHandler();

        app.UseAuthentication();
        app.UseAuthorization();

        // Serves the document at /openapi/v1.json. The service is internal — the gateway has no /openapi
        // route (ADR-0030) — so it is mapped in every environment for local tooling and the codegen emit.
        app.MapOpenApi();

        // RunJasperFxCommands instead of Run so the Wolverine code-generation commands are available
        // (ADR-0034): `dotnet run -- codegen write` regenerates the committed handler code. With no arguments
        // (how Aspire launches the service) it just runs the host. WebApplicationFactory-based tests set
        // JasperFxEnvironment.AutoStartHost so this dispatcher still starts the host they intercept.
        return app.RunJasperFxCommands(args);
    }
}
