using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SmartSolutionsLab.Roomy.Identity.Api.Endpoints;
using SmartSolutionsLab.Roomy.Identity.Api.Hosting;
using SmartSolutionsLab.Roomy.Identity.Api.Seeding;
using SmartSolutionsLab.Roomy.Identity.Infrastructure;
using SmartSolutionsLab.Roomy.Identity.Infrastructure.Keycloak;
using SmartSolutionsLab.Roomy.Infrastructure.Messaging;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// The identity context owns its database (ADR-0014); Aspire injects the connection string by name.
var identityConnectionString = builder.Configuration.GetConnectionString("identity")
    ?? throw new InvalidOperationException("Missing connection string 'identity'.");

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

// The identity API is internal — reached only through the BFF, which forwards the Keycloak access
// token (ADR-0013). Validate it as a JWT bearer against the realm; the BFF owns login/session. The
// audience is not validated (the gateway gates access, and a Keycloak token's audience varies by
// client), but the issuer/realm must match.
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = $"{keycloakBaseAddress.ToString().TrimEnd('/')}/realms/{keycloakRealm}";
        options.RequireHttpsMetadata = false;
        options.TokenValidationParameters.ValidateAudience = false;
    });
builder.Services.AddAuthorization();

// Wolverine's durable transactional outbox/inbox over the identity database, with RabbitMQ as the
// default transport (ADR-0005/0012/0015). The outbox shares the database with the User write so a
// published integration event commits atomically with the aggregate.
builder.AddRoomyMessaging(new MessagingOptions
{
    Transport = MessagingTransport.RabbitMq,
    PostgresConnectionString = identityConnectionString,
    ConnectionString = builder.Configuration.GetConnectionString("rabbitmq")
        ?? throw new InvalidOperationException("Missing connection string 'rabbitmq'."),
});

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

// Apply migrations, then seed — registration order is start order, so the schema exists first.
builder.Services.AddHostedService<IdentityDatabaseMigrator>();
builder.Services.AddHostedService<DefaultAdminSeederHostedService>();

var app = builder.Build();

app.MapDefaultEndpoints();

app.UseAuthentication();
app.UseAuthorization();

app.MapAccountEndpoints();

app.Run();
