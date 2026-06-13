using SmartSolutionsLab.Roomy.Identity.Infrastructure.Persistence;
using SmartSolutionsLab.Roomy.TestSupport;

namespace SmartSolutionsLab.Roomy.Identity.IntegrationTests;

public sealed class PostgresDatabaseFixture
    : ContextPostgresFixture<Projects.Roomy_Identity_TestAppHost, IdentityDbContext>
{
    protected override string DatabaseResourceName => "identity";
}
