using Microsoft.EntityFrameworkCore;
using SmartSolutionsLab.Roomy.Identity.Infrastructure.Persistence;
using SmartSolutionsLab.Roomy.TestSupport;

namespace SmartSolutionsLab.Roomy.Identity.IntegrationTests;

// Provisions a real PostgreSQL via Aspire and applies the identity migrations, so the persistence tests
// exercise the real provider (value converters, unique indexes, NULL semantics) and validate that the
// InitialCreate migration produces the mapped schema. Requires Docker.
public sealed class PostgresDatabaseFixture : BasePostgresFixture<Projects.Roomy_Identity_TestAppHost>
{
    protected override string DatabaseResourceName => "identity";

    protected override async Task CreateSchemaAsync(CancellationToken cancellationToken)
    {
        await using var context = CreateContext();
        await context.Database.MigrateAsync(cancellationToken);
    }

    public IdentityDbContext CreateContext() => new(NpgsqlOptions<IdentityDbContext>());
}
