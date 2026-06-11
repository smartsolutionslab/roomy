using JasperFx;
using JasperFx.CommandLine;
using SmartSolutionsLab.Roomy.Infrastructure.Authentication;
using SmartSolutionsLab.Roomy.Infrastructure.Messaging;
using SmartSolutionsLab.Roomy.Organization.Api;
using SmartSolutionsLab.Roomy.Organization.Api.Endpoints;
using SmartSolutionsLab.Roomy.Organization.Api.Seeding;
using SmartSolutionsLab.Roomy.Organization.Infrastructure;
using SmartSolutionsLab.Roomy.Web.Http;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

var connectionString = builder.Configuration.GetOrganizationConnectionString();

builder.Services.AddOrganizationPersistence(connectionString);

var keycloak = builder.Configuration.GetSection("Keycloak");
var keycloakBaseAddress = new Uri(keycloak["BaseAddress"] ?? throw new InvalidOperationException("Missing configuration 'Keycloak:BaseAddress'."));
var keycloakRealm = keycloak["Realm"] ?? "roomy";

builder.Services.AddKeycloakJwtBearer(keycloakBaseAddress, keycloakRealm)
    .AddOrganizationUseCases()
    .AddOpenApi(options => options.CreateSchemaReferenceId = EndpointSchemaIds.ForEndpointDto);

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
            PostgresConnectionString = connectionString,
            ConnectionString = builder.Configuration.GetRabbitMqConnectionString(),
        },
        applicationAssembly: typeof(OrganizationApiHost).Assembly,
        typeof(SmartSolutionsLab.Roomy.Organization.Infrastructure.Messaging.UserRegisteredConsumer).Assembly);

    builder.Services.AddIntegrationEventOutbox();

    var company = builder.Configuration.GetSection(CompanyOptions.SectionName);
    builder.Services.AddSingleton(new CompanyOptions
    {
        Name = company["Name"] ?? throw new InvalidOperationException("Missing configuration 'Company:Name'."),
    });
    builder.Services
        .AddScoped<CompanySeeder>()
        .AddHostedService<CompanySeederHostedService>();

    // The seeded administrator is hired here so it becomes a first-class employee (ADR-0059). Registered
    // after the company seeder so it runs once the seeded Company exists; the hire saga provisions the
    // identity User + Keycloak and the attendance directory row.
    var defaultAdmin = builder.Configuration.GetSection(DefaultAdminOptions.SectionName);
    builder.Services.AddSingleton(new DefaultAdminOptions
    {
        Email = defaultAdmin["Email"] ?? throw new InvalidOperationException("Missing configuration 'DefaultAdmin:Email'."),
        DisplayName = defaultAdmin["DisplayName"] ?? throw new InvalidOperationException("Missing configuration 'DefaultAdmin:DisplayName'."),
        InitialPassword = defaultAdmin["InitialPassword"] ?? throw new InvalidOperationException("Missing configuration 'DefaultAdmin:InitialPassword'."),
    });
    builder.Services
        .AddScoped<DefaultAdminSeeder>()
        .AddHostedService<DefaultAdminSeederHostedService>();
}

var app = builder.Build();

app.MapDefaultEndpoints();

app.UseAuthentication()
    .UseAuthorization();

app.MapOfficeEndpoints()
    .MapEmployeeEndpoints();

// Serves the document at /openapi/v1.json. The service is internal — the gateway has no /openapi
// route (ADR-0030) — so it is mapped in every environment for local tooling and the codegen emit.
app.MapOpenApi();

return await app.RunJasperFxCommands(args);
