using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SmartSolutionsLab.Roomy.Attendance.Infrastructure;
using SmartSolutionsLab.Roomy.Attendance.Infrastructure.Persistence;
using SmartSolutionsLab.Roomy.DbMigrator;
using SmartSolutionsLab.Roomy.Identity.Infrastructure;
using SmartSolutionsLab.Roomy.Identity.Infrastructure.Persistence;
using SmartSolutionsLab.Roomy.Organization.Infrastructure;
using SmartSolutionsLab.Roomy.Organization.Infrastructure.Persistence;

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();

// Each context contributes its database here (database-per-service, ADR-0014): register its persistence
// so the DbContext resolves, then add it as a migration target. `organization` and `attendance` follow
// the same two lines as they land. Aspire injects each connection string by the database resource name.
var identityConnectionString = builder.Configuration.GetConnectionString("identity")
    ?? throw new InvalidOperationException("Missing connection string 'identity'.");
builder.Services.AddIdentityPersistence(identityConnectionString);
builder.Services.AddMigrationTarget<IdentityDbContext>();

var organizationConnectionString = builder.Configuration.GetConnectionString("organization")
    ?? throw new InvalidOperationException("Missing connection string 'organization'.");
builder.Services.AddOrganizationPersistence(organizationConnectionString);
builder.Services.AddMigrationTarget<OrganizationDbContext>();

var attendanceConnectionString = builder.Configuration.GetConnectionString("attendance")
    ?? throw new InvalidOperationException("Missing connection string 'attendance'.");
builder.Services.AddAttendancePersistence(attendanceConnectionString);
builder.Services.AddMigrationTarget<AttendanceDbContext>();

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
