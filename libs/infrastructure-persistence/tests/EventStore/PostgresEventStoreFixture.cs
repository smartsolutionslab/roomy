using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SmartSolutionsLab.Roomy.Infrastructure.Persistence.EventStore;

namespace SmartSolutionsLab.Roomy.Infrastructure.Persistence.Tests.EventStore;

// Spins up a single real PostgreSQL via Aspire — only the database resource — and creates the event-store
// schema from the EF model once for the test class. Lets the event store run against the real provider,
// including the (stream_id, version) unique constraint whose SQLSTATE 23505 violation the concurrency-race
// test exercises (#67) — a path SQLite cannot reproduce (ADR-0012). Requires Docker; CI has it.
public sealed class PostgresEventStoreFixture : IAsyncLifetime
{
    private const string ServerResourceName = "postgres";
    private const string DatabaseResourceName = "eventstore";

    private DistributedApplication? application;
    private string connectionString = string.Empty;

    public async ValueTask InitializeAsync()
    {
        var builder = await DistributedApplicationTestingBuilder
            .CreateAsync<Projects.Roomy_EventStore_TestAppHost>();

        application = await builder.BuildAsync();
        await application.StartAsync();

        using var readiness = new CancellationTokenSource(TimeSpan.FromMinutes(5));
        var notifications = application.Services.GetRequiredService<ResourceNotificationService>();
        await notifications.WaitForResourceHealthyAsync(ServerResourceName, readiness.Token);

        connectionString = await application.GetConnectionStringAsync(DatabaseResourceName, readiness.Token)
            ?? throw new InvalidOperationException("The Postgres resource produced no connection string.");

        await using var context = CreateContext();
        await context.Database.EnsureCreatedAsync(readiness.Token);
    }

    // A fresh context over the real database; each call gets an independent context so a test can model
    // two concurrent writers on one database.
    internal TestEventStoreDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<TestEventStoreDbContext>().UseNpgsql(connectionString).Options);

    internal EfCoreEventStore CreateEventStore(TestEventStoreDbContext context, IEventSerializer serializer) =>
        new(context, serializer, TimeProvider.System);

    public async ValueTask DisposeAsync()
    {
        if (application is not null)
        {
            await application.DisposeAsync();
        }
    }
}
