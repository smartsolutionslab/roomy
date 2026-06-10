using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SmartSolutionsLab.Roomy.Attendance.Domain.AttendanceDays;
using SmartSolutionsLab.Roomy.Attendance.Infrastructure.Persistence;
using SmartSolutionsLab.Roomy.Attendance.Infrastructure.Projections;
using SmartSolutionsLab.Roomy.Infrastructure.Persistence.EventStore;

namespace SmartSolutionsLab.Roomy.Attendance.IntegrationTests;

// Spins up a single real PostgreSQL via Aspire — only the database resource — and creates the
// attendance event-store schema from the EF model once for the test class. Lets the event-sourced
// repository run against the real provider, including the (stream_id, version) unique constraint that
// guarantees optimistic concurrency (ADR-0012). The migration that builds this schema in production
// lands with the host (T015), mirroring identity; here EnsureCreated suffices. Requires Docker.
public sealed class PostgresEventStoreFixture : IAsyncLifetime
{
    private const string ServerResourceName = "postgres";
    private const string DatabaseResourceName = "attendance";

    private DistributedApplication? application;
    private string connectionString = string.Empty;

    public string ConnectionString => connectionString;

    public async ValueTask InitializeAsync()
    {
        var builder = await DistributedApplicationTestingBuilder
            .CreateAsync<Projects.Roomy_Attendance_TestAppHost>();

        application = await builder.BuildAsync();
        await application.StartAsync();

        using var readiness = new CancellationTokenSource(TimeSpan.FromMinutes(5));
        var notifications = application.Services.GetRequiredService<ResourceNotificationService>();
        await notifications.WaitForResourceHealthyAsync(ServerResourceName, readiness.Token);

        connectionString = await application.GetConnectionStringAsync(DatabaseResourceName, readiness.Token)
            ?? throw new InvalidOperationException("The Postgres resource produced no connection string.");

        await using var context = CreateDbContext();
        await context.Database.EnsureCreatedAsync(readiness.Token);
    }

    // A fresh repository over its own DbContext + event store + occupancy projection, as the app resolves
    // it per request. The projection and event store share the one context so the read-model rows and the
    // events commit in a single transaction (ADR-0038). Each call gets an independent context so a test
    // can model two concurrent writers on one database.
    public AttendanceDayRepository CreateRepository()
    {
        var context = CreateDbContext();
        return new(CreateEventStore(context), new ReservationProjection(context), context);
    }

    // The offline read-model rebuilder over its own context + event store + projection, wired like the
    // repository (one shared context so the truncate, replay, and save commit together).
    public ReservationsReadModelRebuilder CreateRebuilder()
    {
        var context = CreateDbContext();
        return new(CreateEventStore(context), new ReservationProjection(context), context);
    }

    public AttendanceDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AttendanceDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        return new AttendanceDbContext(options);
    }

    private static EfCoreEventStore CreateEventStore(AttendanceDbContext context) =>
        new(context, new JsonEventSerializer(AttendanceEventTypeRegistry.Build()), TimeProvider.System);

    public async ValueTask DisposeAsync()
    {
        if (application is not null)
        {
            await application.DisposeAsync();
        }
    }
}
