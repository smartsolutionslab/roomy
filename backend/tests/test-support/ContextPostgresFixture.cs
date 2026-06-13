using Microsoft.EntityFrameworkCore;

namespace SmartSolutionsLab.Roomy.TestSupport;

// A BasePostgresFixture for a single EF Core DbContext: builds the context against the started Postgres
// and applies its migrations as the schema. Derived fixtures only name their database resource. The
// context is created via its DbContextOptions constructor (the EF Core convention every DbContext follows).
public abstract class ContextPostgresFixture<TAppHost, TContext> : BasePostgresFixture<TAppHost>
    where TAppHost : class
    where TContext : DbContext
{
    public TContext CreateContext() =>
        (TContext)Activator.CreateInstance(typeof(TContext), NpgsqlOptions<TContext>())!;

    protected override async Task CreateSchemaAsync(CancellationToken cancellationToken)
    {
        await using var context = CreateContext();
        await context.Database.MigrateAsync(cancellationToken);
    }
}
