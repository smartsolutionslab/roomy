using Microsoft.EntityFrameworkCore;

namespace SmartSolutionsLab.Roomy.Infrastructure.Persistence.EventStore;

public abstract class EventStoreDbContext(DbContextOptions options) : EfCore.RoomyDbContext(options)
{
    public DbSet<StoredEvent> Events => Set<StoredEvent>();

    protected override void ConfigureContext(ModelBuilder modelBuilder)
    {
        base.ConfigureContext(modelBuilder);

        modelBuilder.ApplyConfiguration(new StoredEventConfiguration());
    }
}
