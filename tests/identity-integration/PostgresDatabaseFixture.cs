using Microsoft.EntityFrameworkCore;
using SmartSolutionsLab.Roomy.Identity.Infrastructure.Persistence;
using SmartSolutionsLab.Roomy.TestSupport;

namespace SmartSolutionsLab.Roomy.Identity.IntegrationTests;

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
