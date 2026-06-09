using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace SmartSolutionsLab.Roomy.DbMigrator;

// Applies the EF migrations for every registered context (ADR-0033): for each target it resolves the
// DbContext in its own scope and calls MigrateAsync, which creates the database if absent and applies
// any pending migrations, and is a no-op when the schema is already current (idempotent). A failure for
// any context propagates so the process exits non-zero and fails the orchestration's completion gate.
public sealed class DatabaseMigrator(
    IServiceProvider services,
    IEnumerable<MigrationTarget> targets,
    ILogger<DatabaseMigrator> logger)
{
    public async Task MigrateAsync(CancellationToken cancellationToken)
    {
        foreach (var target in targets)
        {
            logger.LogInformation("Applying migrations for {Context}.", target.Name);

            await using var scope = services.CreateAsyncScope();
            var dbContext = (DbContext)scope.ServiceProvider.GetRequiredService(target.ContextType);
            await dbContext.Database.MigrateAsync(cancellationToken);

            logger.LogInformation("Migrations applied for {Context}.", target.Name);
        }
    }
}
