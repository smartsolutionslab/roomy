using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
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

builder.Services.AddKeycloakIdentityProvider(
    keycloakBaseAddress,
    new KeycloakAdminOptions
    {
        Realm = keycloak["Realm"] ?? "roomy",
        AdminUsername = keycloak["AdminUsername"]
            ?? throw new InvalidOperationException("Missing configuration 'Keycloak:AdminUsername'."),
        AdminPassword = keycloak["AdminPassword"]
            ?? throw new InvalidOperationException("Missing configuration 'Keycloak:AdminPassword'."),
    });

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

var app = builder.Build();

app.MapDefaultEndpoints();

app.Run();
