using JasperFx;
using JasperFx.CommandLine;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SmartSolutionsLab.Roomy.Infrastructure.Messaging;
using SmartSolutionsLab.Roomy.Organization.Api;
using SmartSolutionsLab.Roomy.Organization.Api.Authentication;
using SmartSolutionsLab.Roomy.Organization.Api.Endpoints;
using SmartSolutionsLab.Roomy.Organization.Api.Seeding;
using SmartSolutionsLab.Roomy.Organization.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// The organization context owns its database (ADR-0014); Aspire injects the connection string by name.
var connectionString = builder.Configuration.GetConnectionString("organization")
    ?? throw new InvalidOperationException("Missing connection string 'organization'.");

builder.Services.AddOrganizationPersistence(connectionString);

// The API is internal — reached only through the BFF, which forwards the Keycloak access token
// (ADR-0013). Validate it as a JWT bearer against the realm; the audience is not validated (the gateway
// gates access), but the issuer/realm must match. Realm roles are flattened to role claims so the
// administrator-only routes can authorize on RequireRole.
var keycloak = builder.Configuration.GetSection("Keycloak");
var keycloakBaseAddress = new Uri(keycloak["BaseAddress"]
    ?? throw new InvalidOperationException("Missing configuration 'Keycloak:BaseAddress'."));
var keycloakRealm = keycloak["Realm"] ?? "roomy";

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = $"{keycloakBaseAddress.ToString().TrimEnd('/')}/realms/{keycloakRealm}";
        options.RequireHttpsMetadata = false;
        options.TokenValidationParameters.ValidateAudience = false;

        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = context =>
            {
                KeycloakRealmRoles.AddRoleClaims(context.Principal);
                return Task.CompletedTask;
            },
        };
    });
builder.Services.AddAuthorization();

builder.Services.AddOrganizationUseCases();

// Publish the OpenAPI document the typed Angular client is generated from (ADR-0018/0036).
builder.Services.AddOpenApi();

// Emitting the OpenAPI spec (ADR-0036) runs the host through `getdocument`. AutoStartHost lets that
// HostFactoryResolver-based tool obtain the built service provider instead of the JasperFx dispatcher
// disposing it first; it is scoped to the emit so normal startup is unaffected. The document is built
// from endpoint metadata alone, so during an emit the messaging runtime and the company seeder — the
// two startups that open a broker/database connection — are skipped, letting the spec emit with no
// Postgres or RabbitMQ.
var emittingOpenApiDocument = builder.Configuration.GetValue<bool>("OpenApi:EmitDocument");
if (emittingOpenApiDocument)
{
    JasperFxEnvironment.AutoStartHost = true;
}

// Wolverine's durable transactional outbox + inbox over the organization database, RabbitMQ transport
// (ADR-0005/0015). It publishes OfficeOpened/RoomAdded/EmployeeHired (drained from domain events at
// commit, ADR-0037) and — since 008 — also consumes identity's provisioning acks
// (UserRegistered/UserProvisioningFailed), so the infrastructure assembly is scanned for those consumers.
// The outbox/inbox share the organization database so a published event or a consumed ack commits
// atomically with the employee write (ADR-0012/0025).
if (!emittingOpenApiDocument)
{
    builder.AddRoomyMessaging(
        new MessagingOptions
        {
            Transport = MessagingTransport.RabbitMq,
            PostgresConnectionString = connectionString,
            ConnectionString = builder.Configuration.GetConnectionString("rabbitmq")
                ?? throw new InvalidOperationException("Missing connection string 'rabbitmq'."),
        },
        applicationAssembly: typeof(OrganizationApiHost).Assembly,
        typeof(SmartSolutionsLab.Roomy.Organization.Infrastructure.Messaging.UserRegisteredConsumer).Assembly);

    // Organization publishes from a state-based unit of work, so it opts into the transactional outbox
    // (ADR-0037). Consume-only hosts (identity, attendance) do not, keeping their handler graph clean.
    builder.Services.AddIntegrationEventOutbox();

    // Seed the single company at startup so offices have a company to belong to (research.md D2). The
    // seeder is idempotent, so it is safe on every restart. The schema is applied out-of-process by the
    // db-migrator before this host starts (Aspire WaitForCompletion, ADR-0033).
    var company = builder.Configuration.GetSection(CompanyOptions.SectionName);
    builder.Services.AddSingleton(new CompanyOptions
    {
        Name = company["Name"]
            ?? throw new InvalidOperationException("Missing configuration 'Company:Name'."),
    });
    builder.Services.AddScoped<CompanySeeder>();
    builder.Services.AddHostedService<CompanySeederHostedService>();
}

var app = builder.Build();

app.MapDefaultEndpoints();

app.UseAuthentication();
app.UseAuthorization();

app.MapOfficeEndpoints();
app.MapEmployeeEndpoints();

// Serves the document at /openapi/v1.json. The service is internal — the gateway has no /openapi
// route (ADR-0030) — so it is mapped in every environment for local tooling and the codegen emit.
app.MapOpenApi();

// RunJasperFxCommands so the Wolverine code-generation commands are available (ADR-0034): the host
// runs from committed, pre-generated code (TypeLoadMode.Static). With no arguments it just runs the host.
return await app.RunJasperFxCommands(args);
