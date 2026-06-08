using Microsoft.EntityFrameworkCore;
using SmartSolutionsLab.Roomy.Infrastructure.Persistence.Outbox;

namespace SmartSolutionsLab.Roomy.Infrastructure.Persistence.EfCore;

/// <summary>
/// The EF Core baseline every per-service database derives from (ADR-0012, ADR-0014:
/// database-per-service). It carries the cross-cutting persistence concerns that are identical for
/// every context — the transactional <see cref="Outbox.OutboxMessage"/> table and the snake_case
/// naming policy — so a context's own <c>DbContext</c> adds only its aggregates and read models.
/// </summary>
public abstract class RoomyDbContext : DbContext
{
    protected RoomyDbContext(DbContextOptions options)
        : base(options)
    {
    }

    /// <summary>The transactional outbox shared by every Roomy service database.</summary>
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfiguration(new OutboxMessageConfiguration());

        ConfigureContext(modelBuilder);

        SnakeCaseNamingConvention.Apply(modelBuilder);
    }

    /// <summary>
    /// Override to add a context's own entity configurations. Called after the shared outbox is
    /// mapped and <em>before</em> the snake_case policy is applied, so context tables are renamed
    /// consistently too.
    /// </summary>
    protected virtual void ConfigureContext(ModelBuilder modelBuilder)
    {
    }
}
