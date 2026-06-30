using SmartSolutionsLab.Roomy.Infrastructure.Cryptography;
using SmartSolutionsLab.Roomy.Infrastructure.Messaging;
using SmartSolutionsLab.Roomy.Organization.Api;
using SmartSolutionsLab.Roomy.Organization.Api.Endpoints;
using SmartSolutionsLab.Roomy.Organization.Api.Seeding;
using SmartSolutionsLab.Roomy.Organization.Infrastructure;
using SmartSolutionsLab.Roomy.Organization.Infrastructure.Messaging;
using SmartSolutionsLab.Roomy.Web.Http;

var builder = WebApplication.CreateBuilder(args);

builder.AddRoomyApiDefaults();

var connectionString = builder.Configuration.GetOrganizationConnectionString();
builder.Services.AddOrganizationPersistence(connectionString)
    .AddOrganizationUseCases();
builder.Services.AddCredentialEncryption(builder.Configuration);

if (!builder.Configuration.IsEmittingOpenApiDocument())
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

app.MapOfficeEndpoints()
    .MapEmployeeEndpoints();

return await app.UseRoomyApiPipeline(args);
