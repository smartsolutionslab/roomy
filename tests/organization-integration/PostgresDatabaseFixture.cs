using Microsoft.EntityFrameworkCore;
using SmartSolutionsLab.Roomy.Organization.Infrastructure.Persistence;
using SmartSolutionsLab.Roomy.TestSupport;

namespace SmartSolutionsLab.Roomy.Organization.IntegrationTests;

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
