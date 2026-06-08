using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using SmartSolutionsLab.Roomy.Infrastructure.Persistence.EventStore;

namespace SmartSolutionsLab.Roomy.Infrastructure.Persistence.Tests.EventStore;

/// <summary>
/// Spins up an isolated, file-less SQLite database (kept alive by an open in-memory connection) and
/// creates the schema from the EF model. Lets the event-store tests run against a real relational
/// provider with a real unique index — and therefore real optimistic-concurrency enforcement — with
/// no Docker or external server.
/// </summary>
internal sealed class SqliteEventStoreFixture : IDisposable
{
    private readonly SqliteConnection connection;
    private readonly DbContextOptions<TestEventStoreDbContext> options;

    public SqliteEventStoreFixture()
    {
        connection = new SqliteConnection("Filename=:memory:");
        connection.Open();

        options = new DbContextOptionsBuilder<TestEventStoreDbContext>()
            .UseSqlite(connection)
            .Options;

        using var context = CreateContext();
        context.Database.EnsureCreated();
    }

    public TestEventStoreDbContext CreateContext() => new(options);

    public EfCoreEventStore CreateEventStore(
        TestEventStoreDbContext context,
        IEventSerializer serializer,
        TimeProvider? timeProvider = null) =>
        new(context, serializer, timeProvider ?? TimeProvider.System);

    public void Dispose() => connection.Dispose();
}
