using Microsoft.EntityFrameworkCore;
using SmartSolutionsLab.Roomy.Organization.Infrastructure.Persistence;
using SmartSolutionsLab.Roomy.TestSupport;

namespace SmartSolutionsLab.Roomy.Organization.IntegrationTests;

// Provisions a real PostgreSQL via Aspire and applies the organization migrations, so the persistence tests
// exercise the real provider (value converters, the owned Room collection, unique indexes) and validate that
// the InitialCreate migration produces the mapped schema. Requires Docker.
public sealed class PostgresDatabaseFixture : BasePostgresFixture<Projects.Roomy_Organization_TestAppHost>
{
    protected override string DatabaseResourceName => "organization";

    protected override async Task CreateSchemaAsync(CancellationToken cancellationToken)
    {
        await using var context = CreateContext();
        await context.Database.MigrateAsync(cancellationToken);
    }

    public OrganizationDbContext CreateContext() => new(NpgsqlOptions<OrganizationDbContext>());
}
