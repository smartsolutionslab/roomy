using Microsoft.EntityFrameworkCore;

namespace SmartSolutionsLab.Roomy.Infrastructure.Persistence.EfCore;

/// <summary>
/// The EF Core baseline every per-service database derives from (ADR-0012, ADR-0014:
/// database-per-service). It carries the cross-cutting persistence concerns that are identical for
/// every context — currently the snake_case naming policy — so a context's own <c>DbContext</c>
/// adds only its aggregates and read models.
/// </summary>
/// <remarks>
/// The transactional outbox is <em>not</em> a table this baseline owns: Wolverine's durable
/// transactional outbox/inbox provides it (ADR-0005, ADR-0012:76), enrolled at the composition root
/// against this context's transaction. The earlier hand-rolled <c>OutboxMessage</c> table (#19) was
/// retired when Wolverine took the outbox over (#20).
/// </remarks>
public abstract class RoomyDbContext(DbContextOptions options) : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        ConfigureContext(modelBuilder);

        SnakeCaseNamingConvention.Apply(modelBuilder);
    }

    /// <summary>
    /// Override to add a context's own entity configurations. Called <em>before</em> the snake_case
    /// policy is applied, so context tables are renamed consistently too.
    /// </summary>
    protected virtual void ConfigureContext(ModelBuilder modelBuilder)
    {
    }
}
