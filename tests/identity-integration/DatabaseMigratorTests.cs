using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Shouldly;
using SmartSolutionsLab.Roomy.DbMigrator;
using SmartSolutionsLab.Roomy.Identity.Infrastructure;
using SmartSolutionsLab.Roomy.Identity.Infrastructure.Persistence;

namespace SmartSolutionsLab.Roomy.Identity.IntegrationTests;

// The shared migration runner (ADR-0033) against real Postgres. It targets a *fresh* database name on
// the same server, so it proves the runner both creates the database and applies the EF migrations, and
// that a second run is a no-op. Identity is the runner's only target today; this suite is its home until
// more contexts join.
public sealed class DatabaseMigratorTests(PostgresDatabaseFixture fixture) : IClassFixture<PostgresDatabaseFixture>
{
    [Fact]
    public async Task Creates_the_database_and_applies_the_migrations_for_a_target()
    {
        var connectionString = FreshDatabaseConnectionString();

        await RunMigratorAsync(connectionString);

        await using var context = ContextFor(connectionString);
        var applied = await context.Database.GetAppliedMigrationsAsync(TestContext.Current.CancellationToken);
        applied.ShouldContain(migration => migration.EndsWith("InitialCreate", StringComparison.Ordinal));

        // The mapped table exists and is queryable — the schema was created, not merely the history table.
        var hasUsers = await context.Users.AnyAsync(TestContext.Current.CancellationToken);
        hasUsers.ShouldBeFalse();
    }

    [Fact]
    public async Task Is_idempotent_on_a_second_run()
    {
        var connectionString = FreshDatabaseConnectionString();

        await RunMigratorAsync(connectionString);
        await RunMigratorAsync(connectionString);

        await using var context = ContextFor(connectionString);
        var applied = await context.Database.GetAppliedMigrationsAsync(TestContext.Current.CancellationToken);
        applied.Count(migration => migration.EndsWith("InitialCreate", StringComparison.Ordinal)).ShouldBe(1);
    }

    private static async Task RunMigratorAsync(string connectionString)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddIdentityPersistence(connectionString);
        services.AddMigrationTarget<IdentityDbContext>();
        services.AddSingleton<DatabaseMigrator>();

        await using var provider = services.BuildServiceProvider();
        await provider.GetRequiredService<DatabaseMigrator>().MigrateAsync(TestContext.Current.CancellationToken);
    }

    private static IdentityDbContext ContextFor(string connectionString) =>
        new(new DbContextOptionsBuilder<IdentityDbContext>().UseNpgsql(connectionString).Options);

    // A unique database on the same server as the fixture, which does not exist yet — so the runner has
    // to create it before it can migrate it.
    private string FreshDatabaseConnectionString() =>
        new NpgsqlConnectionStringBuilder(fixture.ConnectionString)
        {
            Database = $"db_migrator_test_{Guid.NewGuid():N}",
        }.ConnectionString;
}
