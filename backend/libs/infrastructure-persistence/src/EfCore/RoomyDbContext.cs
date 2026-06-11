using Microsoft.EntityFrameworkCore;

namespace SmartSolutionsLab.Roomy.Infrastructure.Persistence.EfCore;

public abstract class RoomyDbContext(DbContextOptions options) : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        ConfigureContext(modelBuilder);

        SnakeCaseNamingConvention.Apply(modelBuilder);
    }

    protected virtual void ConfigureContext(ModelBuilder modelBuilder)
    {
    }
}
