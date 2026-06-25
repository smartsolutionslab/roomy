using JasperFx;
using JasperFx.CommandLine;
using SmartSolutionsLab.Roomy.Infrastructure.Authentication;
using SmartSolutionsLab.Roomy.Infrastructure.Messaging;
using SmartSolutionsLab.Roomy.Organization.Api;
using SmartSolutionsLab.Roomy.Organization.Api.Endpoints;
using SmartSolutionsLab.Roomy.Organization.Api.Seeding;
using SmartSolutionsLab.Roomy.Organization.Infrastructure;
using SmartSolutionsLab.Roomy.Organization.Infrastructure.Messaging;
using SmartSolutionsLab.Roomy.Web.Http;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

var connectionString = builder.Configuration.GetOrganizationConnectionString();

builder.Services.AddOrganizationPersistence(connectionString);

var (keycloakBaseAddress, keycloakRealm) = builder.Configuration.ReadKeycloak();

builder.Services.AddKeycloakJwtBearer(keycloakBaseAddress, keycloakRealm)
    .AddOrganizationUseCases()
    .AddOpenApi(options => options.CreateSchemaReferenceId = EndpointSchemaIds.ForEndpointDto);

builder.Services.AddRoomyExceptionHandling();

var emittingOpenApiDocument = builder.Configuration.IsEmittingOpenApiDocument();
if (emittingOpenApiDocument)
{
    JasperFxEnvironment.AutoStartHost = true;
}

if (!emittingOpenApiDocument)
{
    builder.AddRoomyMessaging(connectionString, typeof(OrganizationApiHost).Assembly, typeof(UserRegisteredConsumer).Assembly);

    builder.Services.AddIntegrationEventOutbox();

    builder.Services.AddValidatedOptions<CompanyOptions>(builder.Configuration, CompanyOptions.SectionName);
    builder.Services.AddScoped<CompanySeeder>()
        .AddHostedService<CompanySeederHostedService>();

    // The seeded administrator is hired here so it becomes a first-class employee (ADR-0059). Registered
    // after the company seeder so it runs once the seeded Company exists; the hire saga provisions the
    // identity User + Keycloak and the attendance directory row.
    builder.Services.AddValidatedOptions<DefaultAdminOptions>(builder.Configuration, DefaultAdminOptions.SectionName);
    builder.Services
        .AddScoped<DefaultAdminSeeder>()
        .AddHostedService<DefaultAdminSeederHostedService>();
}

var app = builder.Build();

app.MapDefaultEndpoints();

app.UseExceptionHandler();

app.UseAuthentication()
    .UseAuthorization();

app.MapOfficeEndpoints()
    .MapEmployeeEndpoints();

// Serves the document at /openapi/v1.json. The service is internal — the gateway has no /openapi
// route (ADR-0030) — so it is mapped in every environment for local tooling and the codegen emit.
app.MapOpenApi();

return await app.RunJasperFxCommands(args);
