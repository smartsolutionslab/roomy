using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using SmartSolutionsLab.Roomy.Organization.Infrastructure.Persistence;

namespace SmartSolutionsLab.Roomy.Organization.Api;

// Lets `dotnet ef migrations` build the OrganizationDbContext without booting the host (which would try
// to reach Postgres). The connection string is a design-time placeholder — migrations are scaffolded
// from the EF model, not from a live database.
internal sealed class OrganizationDbContextDesignTimeFactory
    : IDesignTimeDbContextFactory<OrganizationDbContext>
{
    public OrganizationDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<OrganizationDbContext>()
            .UseNpgsql("Host=localhost;Database=organization;Username=postgres;Password=postgres")
            .Options;

        return new OrganizationDbContext(options);
    }
}
