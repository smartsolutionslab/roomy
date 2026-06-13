using SmartSolutionsLab.Roomy.Organization.Infrastructure.Persistence;
using SmartSolutionsLab.Roomy.TestSupport;

namespace SmartSolutionsLab.Roomy.Organization.IntegrationTests;

public sealed class PostgresDatabaseFixture
    : ContextPostgresFixture<Projects.Roomy_Organization_TestAppHost, OrganizationDbContext>
{
    protected override string DatabaseResourceName => "organization";
}
