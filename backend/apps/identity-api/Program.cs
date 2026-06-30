using SmartSolutionsLab.Roomy.Identity.Api;
using SmartSolutionsLab.Roomy.Identity.Api.Endpoints;
using SmartSolutionsLab.Roomy.Identity.Infrastructure;
using SmartSolutionsLab.Roomy.Identity.Infrastructure.Keycloak;
using SmartSolutionsLab.Roomy.Identity.Infrastructure.Messaging;
using SmartSolutionsLab.Roomy.Infrastructure.Cryptography;
using SmartSolutionsLab.Roomy.Infrastructure.Messaging;
using SmartSolutionsLab.Roomy.Web.Http;

var builder = WebApplication.CreateBuilder(args);

var (keycloakBaseAddress, keycloakAdmin) = builder.Configuration.ReadKeycloakAdmin();

builder.AddRoomyApiDefaults(keycloakBaseAddress, keycloakAdmin.Realm);

var identityConnectionString = builder.Configuration.GetIdentityConnectionString();
builder.Services.AddIdentityPersistence(identityConnectionString);
builder.Services.AddKeycloakIdentityProvider(keycloakBaseAddress, keycloakAdmin);
builder.Services.AddIdentityUseCases();
builder.Services.AddCredentialEncryption(builder.Configuration);

if (!builder.Configuration.IsEmittingOpenApiDocument())
{
    builder.AddRoomyMessaging(identityConnectionString, typeof(IdentityApiHost).Assembly, typeof(EmployeeHiredConsumer).Assembly);

    builder.Services.AddIntegrationEventOutbox();
}

var app = builder.Build();

app.MapAccountEndpoints();
app.MapAdminUserEndpoints();

return await app.UseRoomyApiPipeline(args);
