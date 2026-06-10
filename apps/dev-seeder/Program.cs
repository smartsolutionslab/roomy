using SmartSolutionsLab.Roomy.Attendance.Infrastructure;
using SmartSolutionsLab.Roomy.DevSeeder;
using SmartSolutionsLab.Roomy.Identity.Infrastructure;
using SmartSolutionsLab.Roomy.Identity.Infrastructure.Keycloak;
using SmartSolutionsLab.Roomy.Organization.Infrastructure;

// A one-shot DEV seeding tool (run on demand, e.g. an explicit-start Aspire resource): it writes the
// "Obex Labs" demo dataset into the three dev databases and provisions each colleague's Keycloak login.
// It reuses each context's persistence registration + the Keycloak admin provider, like the db-migrator.
var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();

var identityConnectionString = builder.Configuration.GetIdentityConnectionString();
builder.Services.AddIdentityPersistence(identityConnectionString);

var organizationConnectionString = builder.Configuration.GetOrganizationConnectionString();
builder.Services.AddOrganizationPersistence(organizationConnectionString);

var attendanceConnectionString = builder.Configuration.GetAttendanceConnectionString();
builder.Services.AddAttendancePersistence(attendanceConnectionString);

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

var seed = builder.Configuration.GetSection("Seed");
var companyId = Guid.Parse(seed["CompanyId"]
    ?? throw new InvalidOperationException("Missing configuration 'Seed:CompanyId'."));
builder.Services.AddSingleton(new SeedOptions(companyId, seed["EmployeePassword"] ?? "ObexLabs.2025"));
builder.Services.AddScoped<Seeder>();

using var host = builder.Build();
using var scope = host.Services.CreateScope();
var seeder = scope.ServiceProvider.GetRequiredService<Seeder>();
var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

try
{
    await seeder.SeedAsync(CancellationToken.None);
    return 0;
}
catch (Exception exception)
{
    logger.LogError(exception, "Dev seeding failed.");
    return 1;
}
