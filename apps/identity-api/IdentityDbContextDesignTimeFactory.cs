using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using SmartSolutionsLab.Roomy.Identity.Infrastructure.Persistence;

namespace SmartSolutionsLab.Roomy.Identity.Api;

// Lets `dotnet ef migrations` build the IdentityDbContext without booting the host (which would try to
// reach Postgres, RabbitMQ, and Keycloak). The connection string is a design-time placeholder —
// migrations are scaffolded from the EF model, not from a live database.
internal sealed class IdentityDbContextDesignTimeFactory : IDesignTimeDbContextFactory<IdentityDbContext>
{
    public IdentityDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<IdentityDbContext>()
            .UseNpgsql("Host=localhost;Database=identity;Username=postgres;Password=postgres")
            .Options;

        return new IdentityDbContext(options);
    }
}
