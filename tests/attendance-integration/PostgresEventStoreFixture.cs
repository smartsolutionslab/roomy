using Microsoft.EntityFrameworkCore;
using SmartSolutionsLab.Roomy.Attendance.Infrastructure.Persistence;
using SmartSolutionsLab.Roomy.Attendance.Infrastructure.Projections;
using SmartSolutionsLab.Roomy.Infrastructure.Persistence.EventStore;
using SmartSolutionsLab.Roomy.TestSupport;

namespace SmartSolutionsLab.Roomy.Attendance.IntegrationTests;

// Provisions a real PostgreSQL via Aspire and applies the attendance migrations, so the event-sourced
// repository and read models run against the real provider — including the (stream_id, version) unique
// constraint that guarantees optimistic concurrency (ADR-0012) and the pg_trgm/unaccent extensions, the
// immutable_unaccent wrapper, and the GIN trigram index the employee search relies on (ADR-0047, research R7:
// the migrations themselves must be exercised so a missing extension fails here, not in production). Each
// Create* call gets an independent context so a test can model two concurrent writers on one database.
// Requires Docker.
public sealed class PostgresEventStoreFixture : BasePostgresFixture<Projects.Roomy_Attendance_TestAppHost>
{
    protected override string DatabaseResourceName => "attendance";

    protected override async Task CreateSchemaAsync(CancellationToken cancellationToken)
    {
        await using var context = CreateDbContext();
        await context.Database.MigrateAsync(cancellationToken);
    }

    // A fresh repository over its own DbContext + event store + occupancy projection, as the app resolves it
    // per request; the projection and event store share the one context so the read-model rows and the events
    // commit in a single transaction (ADR-0038).
    public AttendanceDayRepository CreateRepository()
    {
        var context = CreateDbContext();
        return new(CreateEventStore(context), new ReservationProjection(context), context);
    }

    // The offline read-model rebuilder, wired like the repository (one shared context so the truncate, replay,
    // and save commit together).
    public ReservationsReadModelRebuilder CreateRebuilder()
    {
        var context = CreateDbContext();
        return new(CreateEventStore(context), new ReservationProjection(context), context);
    }

    public AttendanceDbContext CreateDbContext() => new(NpgsqlOptions<AttendanceDbContext>());

    private static EfCoreEventStore CreateEventStore(AttendanceDbContext context) =>
        new(context, new JsonEventSerializer(AttendanceEventTypeRegistry.Build()), TimeProvider.System);
}
