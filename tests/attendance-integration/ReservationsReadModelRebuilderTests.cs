using Microsoft.EntityFrameworkCore;
using Shouldly;
using SmartSolutionsLab.Roomy.Attendance.Domain.AttendanceDays;

namespace SmartSolutionsLab.Roomy.Attendance.IntegrationTests;

// The Reservations read model is a derived cache with no invariants (ADR-0026/0038, research R5): inline
// projection only moves it forward, so a projector fix or recovery needs a rebuild that re-derives it from
// the source of truth — the company-day event streams. This pins that the rebuild is a pure function of
// the log: after truncating and replaying every stream, the read model is identical to the one the inline
// forward projection produced. Runs against real PostgreSQL (the streams + the read model).
public sealed class ReservationsReadModelRebuilderTests(PostgresEventStoreFixture fixture)
    : IClassFixture<PostgresEventStoreFixture>
{
    private static readonly BookingDate bookingDate =
        BookingDate.From(FirstMondayOnOrAfter(new DateOnly(2026, 6, 1)));

    private static readonly DateTimeOffset occurredAt = new(2026, 6, 8, 9, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Rebuild_reproduces_the_forward_projected_read_model()
    {
        // Seed several company-day streams via the write path (inline forward projection), including a
        // cancellation so the net read-model state is non-trivial and spans more than one stream.
        var company = CompanyIdentifier.New();
        var room = SomeRoom();
        await ReserveAsync(company, EmployeeIdentifier.New(), room);

        var cancelledEmployee = EmployeeIdentifier.New();
        var toCancel = await ReserveAsync(company, cancelledEmployee, room);
        await CancelAsync(company, toCancel, cancelledEmployee);

        var otherCompany = CompanyIdentifier.New();
        await ReserveAsync(otherCompany, EmployeeIdentifier.New(), SomeRoom());

        var forwardProjected = await SnapshotAsync();
        forwardProjected.ShouldNotBeEmpty();

        await fixture.CreateRebuilder().RebuildAsync(TestContext.Current.CancellationToken);

        var rebuilt = await SnapshotAsync();
        rebuilt.ShouldBe(forwardProjected);
    }

    private async Task<IReadOnlyList<ReservationSnapshot>> SnapshotAsync()
    {
        await using var query = fixture.CreateDbContext();
        var rows = await query.Reservations.AsNoTracking().ToListAsync(TestContext.Current.CancellationToken);
        return rows
            .Select(row => new ReservationSnapshot(
                row.ReservationId, row.CompanyId, row.EmployeeId, row.OfficeId, row.RoomId, row.Date))
            .OrderBy(snapshot => snapshot.Reservation)
            .ToList();
    }

    private async Task<ReservationIdentifier> ReserveAsync(
        CompanyIdentifier company,
        EmployeeIdentifier employee,
        RoomReference room)
    {
        var day = await fixture.CreateRepository().LoadAsync(company, bookingDate, TestContext.Current.CancellationToken);
        var decision = day.Reserve(employee, room, RoomCapacity.From(8), bookingDate, occurredAt);
        decision.IsSuccess.ShouldBeTrue();
        (await fixture.CreateRepository().SaveAsync(day, TestContext.Current.CancellationToken)).IsSuccess.ShouldBeTrue();
        return decision.Value;
    }

    private async Task CancelAsync(
        CompanyIdentifier company,
        ReservationIdentifier reservation,
        EmployeeIdentifier employee)
    {
        var day = await fixture.CreateRepository().LoadAsync(company, bookingDate, TestContext.Current.CancellationToken);
        day.Cancel(reservation, employee, actorIsAdmin: false, bookingDate, occurredAt).IsSuccess.ShouldBeTrue();
        (await fixture.CreateRepository().SaveAsync(day, TestContext.Current.CancellationToken)).IsSuccess.ShouldBeTrue();
    }

    private static RoomReference SomeRoom() => RoomReference.From(OfficeIdentifier.New(), RoomIdentifier.New());

    private static DateOnly FirstMondayOnOrAfter(DateOnly start)
    {
        var date = start;
        while (date.DayOfWeek != DayOfWeek.Monday)
        {
            date = date.AddDays(1);
        }

        return date;
    }

    private sealed record ReservationSnapshot(
        Guid Reservation,
        Guid Company,
        Guid Employee,
        Guid Office,
        Guid Room,
        DateOnly Date);
}
