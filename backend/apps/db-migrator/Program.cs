using SmartSolutionsLab.Roomy.Attendance.Infrastructure;
using SmartSolutionsLab.Roomy.Attendance.Infrastructure.Persistence;
using SmartSolutionsLab.Roomy.DbMigrator;
using SmartSolutionsLab.Roomy.Identity.Infrastructure;
using SmartSolutionsLab.Roomy.Identity.Infrastructure.Persistence;
using SmartSolutionsLab.Roomy.Organization.Infrastructure;
using SmartSolutionsLab.Roomy.Organization.Infrastructure.Persistence;

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();

var identityConnectionString = builder.Configuration.GetIdentityConnectionString();
builder.Services.AddIdentityPersistence(identityConnectionString).AddMigrationTarget<IdentityDbContext>();

var organizationConnectionString = builder.Configuration.GetOrganizationConnectionString();
builder.Services.AddOrganizationPersistence(organizationConnectionString).AddMigrationTarget<OrganizationDbContext>();

var attendanceConnectionString = builder.Configuration.GetAttendanceConnectionString();
builder.Services.AddAttendancePersistence(attendanceConnectionString).AddMigrationTarget<AttendanceDbContext>();

builder.Services.AddSingleton<DatabaseMigrator>();

using var host = builder.Build();

var migrator = host.Services.GetRequiredService<DatabaseMigrator>();
var logger = host.Services.GetRequiredService<ILogger<DatabaseMigrator>>();

try
{
    await migrator.MigrateAsync(CancellationToken.None);
    return 0;
}
catch (Exception exception)
{
    logger.LogError(exception, "Database migration failed; the schema was not fully applied.");
    return 1;
}
