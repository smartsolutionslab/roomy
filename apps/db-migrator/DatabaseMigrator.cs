using Microsoft.EntityFrameworkCore;

namespace SmartSolutionsLab.Roomy.DbMigrator;

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
