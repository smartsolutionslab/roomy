using Microsoft.EntityFrameworkCore;

namespace SmartSolutionsLab.Roomy.Infrastructure.Persistence.EventStore;

/// <summary>
/// The <see cref="EfCore.RoomyDbContext"/> baseline for an <em>event-sourced</em> context: it adds
/// the append-only <see cref="StoredEvent"/> table on top of the shared outbox and naming policy.
/// A state-based context derives from <see cref="EfCore.RoomyDbContext"/> directly and never gains
/// the events table; an event-sourced one derives from this and pairs it with
/// <see cref="EfCoreEventStore"/> (ADR-0012).
/// </summary>
public abstract class EventStoreDbContext : EfCore.RoomyDbContext
{
    protected EventStoreDbContext(DbContextOptions options)
        : base(options)
    {
    }

    /// <summary>The append-only event log for this context's aggregates.</summary>
    public DbSet<StoredEvent> Events => Set<StoredEvent>();

    protected override void ConfigureContext(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        base.ConfigureContext(modelBuilder);

        modelBuilder.ApplyConfiguration(new StoredEventConfiguration());
    }
}
