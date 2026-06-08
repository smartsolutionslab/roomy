using Microsoft.EntityFrameworkCore;
using SmartSolutionsLab.Roomy.Identity.Domain.Users;
using SmartSolutionsLab.Roomy.Infrastructure.Persistence.EfCore;

namespace SmartSolutionsLab.Roomy.Identity.Infrastructure.Persistence;

// The identity context's own database (database-per-service, ADR-0014). It derives from the shared
// RoomyDbContext baseline (snake_case naming) and adds the User aggregate. Identity is state-based,
// not event-sourced (ADR-0012), so it derives from RoomyDbContext directly rather than the event
// store context.
public sealed class IdentityDbContext(DbContextOptions<IdentityDbContext> options)
    : RoomyDbContext(options)
{
    public DbSet<User> Users => Set<User>();

    protected override void ConfigureContext(ModelBuilder modelBuilder)
    {
        base.ConfigureContext(modelBuilder);

        modelBuilder.ApplyConfiguration(new UserConfiguration());
    }
}
