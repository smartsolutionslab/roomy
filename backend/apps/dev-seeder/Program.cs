using SmartSolutionsLab.Roomy.Attendance.Infrastructure;
using SmartSolutionsLab.Roomy.DevSeeder;
using SmartSolutionsLab.Roomy.Identity.Infrastructure;
using SmartSolutionsLab.Roomy.Identity.Infrastructure.Keycloak;
using SmartSolutionsLab.Roomy.Infrastructure.Cryptography;
using SmartSolutionsLab.Roomy.Organization.Infrastructure;

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();

var identityConnectionString = builder.Configuration.GetIdentityConnectionString();
builder.Services.AddIdentityPersistence(identityConnectionString);

var organizationConnectionString = builder.Configuration.GetOrganizationConnectionString();
builder.Services.AddOrganizationPersistence(organizationConnectionString);

var attendanceConnectionString = builder.Configuration.GetAttendanceConnectionString();
builder.Services.AddAttendancePersistence(attendanceConnectionString);

var (keycloakBaseAddress, keycloakAdmin) = builder.Configuration.ReadKeycloakAdmin();
builder.Services.AddKeycloakIdentityProvider(keycloakBaseAddress, keycloakAdmin);
builder.Services.AddCredentialEncryption(builder.Configuration);

var seed = builder.Configuration.GetSection("Seed");
var companyId = Guid.Parse(seed["CompanyId"] ?? throw new InvalidOperationException("Missing configuration 'Seed:CompanyId'."));
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
