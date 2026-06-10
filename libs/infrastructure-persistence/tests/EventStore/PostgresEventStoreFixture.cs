using SmartSolutionsLab.Roomy.Infrastructure.Persistence.EventStore;
using SmartSolutionsLab.Roomy.TestSupport;

namespace SmartSolutionsLab.Roomy.Infrastructure.Persistence.Tests.EventStore;

// Provisions a real PostgreSQL via Aspire and creates the event-store schema from the EF model, so the event
// store runs against the real provider — including the (stream_id, version) unique constraint whose SQLSTATE
// 23505 violation the concurrency-race test exercises (#67), a path SQLite cannot reproduce (ADR-0012).
// Requires Docker.
public sealed class PostgresEventStoreFixture : BasePostgresFixture<Projects.Roomy_EventStore_TestAppHost>
{
    protected override string DatabaseResourceName => "eventstore";

    protected override async Task CreateSchemaAsync(CancellationToken cancellationToken)
    {
        await using var context = CreateContext();
        await context.Database.EnsureCreatedAsync(cancellationToken);
    }

    // A fresh context over the real database; each call gets an independent context so a test can model two
    // concurrent writers on one database.
    internal TestEventStoreDbContext CreateContext() => new(NpgsqlOptions<TestEventStoreDbContext>());

    internal EfCoreEventStore CreateEventStore(TestEventStoreDbContext context, IEventSerializer serializer) =>
        new(context, serializer, TimeProvider.System);
}
