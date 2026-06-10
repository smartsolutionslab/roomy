using JasperFx;
using JasperFx.CommandLine;
using SmartSolutionsLab.Roomy.Identity.Api;
using SmartSolutionsLab.Roomy.Identity.Api.Endpoints;
using SmartSolutionsLab.Roomy.Identity.Api.Seeding;
using SmartSolutionsLab.Roomy.Identity.Infrastructure;
using SmartSolutionsLab.Roomy.Identity.Infrastructure.Keycloak;
using SmartSolutionsLab.Roomy.Identity.Infrastructure.Messaging;
using SmartSolutionsLab.Roomy.Infrastructure.Authentication;
using SmartSolutionsLab.Roomy.Infrastructure.Messaging;
using SmartSolutionsLab.Roomy.Web.Http;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// The identity context owns its database (ADR-0014); Aspire injects the connection string by name.
var identityConnectionString = builder.Configuration.GetIdentityConnectionString();

builder.Services.AddIdentityPersistence(identityConnectionString);

// Keycloak owns credentials (ADR-0013); the host binds its base address, realm, and admin credentials
// from configuration (Aspire / user secrets) — never hard-coded.
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

// The identity API is internal — reached only through the BFF, which forwards the Keycloak access token
// (ADR-0013). Validate it as a JWT bearer against the realm; the BFF owns login/session. Shared across the
// context API hosts (ADR-0045).
builder.Services.AddKeycloakJwtBearer(keycloakBaseAddress, keycloakRealm);

// Publish the OpenAPI document the typed Angular client is generated from (ADR-0018/0036).
builder.Services.AddOpenApi(options => options.CreateSchemaReferenceId = EndpointSchemaIds.ForEndpointDto);

// The provisioning use cases (US3, ADR-0025), bound to their owned command-handler ports — the
// EmployeeHired consumer resolves RegisterUser through these.
builder.Services.AddIdentityUseCases();

// Emitting the OpenAPI spec (ADR-0036) runs the host through `getdocument`. AutoStartHost lets that
// HostFactoryResolver-based tool obtain the built service provider instead of the JasperFx dispatcher
// disposing it first; it is scoped to the emit so the Wolverine `codegen write` step and normal
// startup are unaffected. The document is built from endpoint metadata alone, so during an emit the
// messaging runtime and the DefaultAdmin seeder — the two startups that open a broker/database
// connection — are skipped, letting the spec emit with no Postgres or RabbitMQ.
var emittingOpenApiDocument = builder.Configuration.GetValue<bool>("OpenApi:EmitDocument");
if (emittingOpenApiDocument)
{
    JasperFxEnvironment.AutoStartHost = true;
}

// Wolverine's durable transactional outbox/inbox over the identity database, with RabbitMQ as the
// default transport (ADR-0005/0012/0015). The outbox shares the database with the User write so a
// published integration event commits atomically with the aggregate. The identity infrastructure
// assembly is scanned for consumers so EmployeeHired (organization's published language) is handled.
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

// Seed the DefaultAdmin at startup so the system is administrable from first run (FR-004, research
// R4). The seeder is idempotent, so it is safe on every restart.
var defaultAdmin = builder.Configuration.GetSection(DefaultAdminOptions.SectionName);
builder.Services.AddSingleton(new DefaultAdminOptions
{
    Email = defaultAdmin["Email"]
        ?? throw new InvalidOperationException("Missing configuration 'DefaultAdmin:Email'."),
    DisplayName = defaultAdmin["DisplayName"]
        ?? throw new InvalidOperationException("Missing configuration 'DefaultAdmin:DisplayName'."),
    InitialPassword = defaultAdmin["InitialPassword"]
        ?? throw new InvalidOperationException("Missing configuration 'DefaultAdmin:InitialPassword'."),
});
builder.Services.AddScoped<DefaultAdminSeeder>();

// The schema is applied out-of-process by the db-migrator before this host starts (Aspire
// WaitForCompletion, ADR-0033), so the seeder can query the users table straight away.
if (!emittingOpenApiDocument)
{
    builder.Services.AddHostedService<DefaultAdminSeederHostedService>();
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
