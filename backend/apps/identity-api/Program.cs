using JasperFx;
using JasperFx.CommandLine;
using SmartSolutionsLab.Roomy.Identity.Api;
using SmartSolutionsLab.Roomy.Identity.Api.Endpoints;
using SmartSolutionsLab.Roomy.Identity.Infrastructure;
using SmartSolutionsLab.Roomy.Identity.Infrastructure.Keycloak;
using SmartSolutionsLab.Roomy.Identity.Infrastructure.Messaging;
using SmartSolutionsLab.Roomy.Infrastructure.Authentication;
using SmartSolutionsLab.Roomy.Infrastructure.Messaging;
using SmartSolutionsLab.Roomy.Web.Http;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

var identityConnectionString = builder.Configuration.GetIdentityConnectionString();

builder.Services.AddIdentityPersistence(identityConnectionString);

var keycloak = builder.Configuration.GetSection("Keycloak");
var keycloakBaseAddress = new Uri(keycloak["BaseAddress"]
    ?? throw new InvalidOperationException("Missing configuration 'Keycloak:BaseAddress'."));
var keycloakRealm = keycloak["Realm"] ?? "roomy";

builder.Services.AddKeycloakIdentityProvider(
    keycloakBaseAddress,
    new KeycloakAdminOptions
    {
        Realm = keycloakRealm,
        AdminUsername = keycloak["AdminUsername"]
            ?? throw new InvalidOperationException("Missing configuration 'Keycloak:AdminUsername'."),
        AdminPassword = keycloak["AdminPassword"]
            ?? throw new InvalidOperationException("Missing configuration 'Keycloak:AdminPassword'."),
    });

builder.Services.AddKeycloakJwtBearer(keycloakBaseAddress, keycloakRealm);

builder.Services.AddOpenApi(options => options.CreateSchemaReferenceId = EndpointSchemaIds.ForEndpointDto);

builder.Services.AddIdentityUseCases();

// Emitting the OpenAPI spec (ADR-0036) runs the host through `getdocument`. AutoStartHost lets that
// HostFactoryResolver-based tool obtain the built service provider instead of the JasperFx dispatcher
// disposing it first; it is scoped to the emit so the Wolverine `codegen write` step and normal
// startup are unaffected. The document is built from endpoint metadata alone, so during an emit the
// messaging runtime — the startup that opens a broker/database connection — is skipped, letting the
// spec emit with no Postgres or RabbitMQ.
var emittingOpenApiDocument = builder.Configuration.GetValue<bool>("OpenApi:EmitDocument");
if (emittingOpenApiDocument)
{
    JasperFxEnvironment.AutoStartHost = true;
}

if (!emittingOpenApiDocument)
{
    builder.AddRoomyMessaging(
        new MessagingOptions
        {
            Transport = MessagingTransport.RabbitMq,
            PostgresConnectionString = identityConnectionString,
            ConnectionString = builder.Configuration.GetRabbitMqConnectionString(),
        },
        applicationAssembly: typeof(IdentityApiHost).Assembly,
        typeof(EmployeeHiredConsumer).Assembly);
}

var app = builder.Build();

app.MapDefaultEndpoints();

app.UseAuthentication();
app.UseAuthorization();

app.MapAccountEndpoints();
app.MapAdminUserEndpoints();

// Serves the document at /openapi/v1.json. The service is internal — the gateway has no /openapi
// route (ADR-0030) — so it is mapped in every environment for local tooling and the codegen emit.
app.MapOpenApi();

// RunJasperFxCommands instead of Run so the Wolverine code-generation commands are available
// (ADR-0034): `dotnet run -- codegen write` regenerates the committed handler code. With no arguments
// (how Aspire launches the service) it just runs the host. WebApplicationFactory-based tests set
// JasperFxEnvironment.AutoStartHost so this dispatcher still starts the host they intercept.
return await app.RunJasperFxCommands(args);
