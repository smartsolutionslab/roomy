using Microsoft.EntityFrameworkCore;
using Shouldly;
using SmartSolutionsLab.Roomy.Attendance.Domain.AttendanceDays;

namespace SmartSolutionsLab.Roomy.Attendance.IntegrationTests;

// The occupancy projection against real PostgreSQL (004, ADR-0038): the Reservations read model is
// maintained inline, in the same transaction as the AttendanceDay event append, so a placed reservation
// is queryable the moment it commits (FR-010) and a cancellation removes it. The reserve-after-conflict
// case pins the correctness of the change-tracker reset (research R4): a losing optimistic-retry attempt
// must not leak a row into the winning one.
public sealed class ReservationProjectionTests(PostgresEventStoreFixture fixture)
    : IClassFixture<PostgresEventStoreFixture>
{
    private static readonly BookingDate bookingDate =
        BookingDate.From(FirstMondayOnOrAfter(new DateOnly(2026, 6, 1)));

    private static readonly DateTimeOffset occurredAt = new(2026, 6, 8, 9, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task A_placed_reservation_is_projected_into_the_read_model()
    {
        var company = CompanyIdentifier.New();
        var employee = EmployeeIdentifier.New();
        var room = SomeRoom();

        var reservationId = await ReserveAsync(company, employee, room);

        await using var query = fixture.CreateDbContext();
        var row = await query.Reservations
            .SingleAsync(reservation => reservation.ReservationId == reservationId.Value, TestContext.Current.CancellationToken);

        row.EmployeeId.ShouldBe(employee.Value);
        row.RoomId.ShouldBe(room.Room.Value);
        row.OfficeId.ShouldBe(room.Office.Value);
        row.Date.ShouldBe(bookingDate.Value);
    }

    [Fact]
    public async Task A_cancelled_reservation_is_removed_from_the_read_model()
    {
        var company = CompanyIdentifier.New();
        var employee = EmployeeIdentifier.New();
        var room = SomeRoom();
        var reservationId = await ReserveAsync(company, employee, room);

        var day = await fixture.CreateRepository().LoadAsync(company, bookingDate, TestContext.Current.CancellationToken);
        day.Cancel(reservationId, employee, actorIsAdmin: false, bookingDate, occurredAt).IsSuccess.ShouldBeTrue();
        (await fixture.CreateRepository().SaveAsync(day, TestContext.Current.CancellationToken)).IsSuccess.ShouldBeTrue();

        await using var query = fixture.CreateDbContext();
        var remaining = await query.Reservations
            .CountAsync(reservation => reservation.ReservationId == reservationId.Value, TestContext.Current.CancellationToken);

        remaining.ShouldBe(0);
    }

    [Fact]
    public async Task A_losing_retry_attempt_does_not_leak_a_row_into_the_winning_one()
    {
        var company = CompanyIdentifier.New();
        var room = SomeRoom();
        var mainEmployee = EmployeeIdentifier.New();

        // The main writer reuses one repository (one scoped context) across attempts, as the application's
        // bounded retry does. It loads the empty day and decides to reserve.
        var mainWriter = fixture.CreateRepository();
        var firstAttempt = await mainWriter.LoadAsync(company, bookingDate, TestContext.Current.CancellationToken);
        firstAttempt.Reserve(mainEmployee, room, RoomCapacity.From(8), bookingDate, occurredAt);

        // Another writer commits to the same company-day first, so the main writer's save loses on version.
        var otherWriter = fixture.CreateRepository();
        var otherDay = await otherWriter.LoadAsync(company, bookingDate, TestContext.Current.CancellationToken);
        otherDay.Reserve(EmployeeIdentifier.New(), room, RoomCapacity.From(8), bookingDate, occurredAt);
        (await otherWriter.SaveAsync(otherDay, TestContext.Current.CancellationToken)).IsSuccess.ShouldBeTrue();

        var lost = await mainWriter.SaveAsync(firstAttempt, TestContext.Current.CancellationToken);
        lost.IsFailure.ShouldBeTrue();

        // Retry on the same writer: reload fresh state, re-decide, save — this one wins.
        var secondAttempt = await mainWriter.LoadAsync(company, bookingDate, TestContext.Current.CancellationToken);
        secondAttempt.Reserve(mainEmployee, room, RoomCapacity.From(8), bookingDate, occurredAt);
        (await mainWriter.SaveAsync(secondAttempt, TestContext.Current.CancellationToken)).IsSuccess.ShouldBeTrue();

        // Exactly two rows for the day — the other writer's and the main writer's single winning row. The
        // losing attempt's staged row was discarded by the change-tracker reset (no orphan third row).
        await using var query = fixture.CreateDbContext();
        var rowsForDay = await query.Reservations
            .CountAsync(
                reservation => reservation.CompanyId == company.Value && reservation.Date == bookingDate.Value,
                TestContext.Current.CancellationToken);

        rowsForDay.ShouldBe(2);
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
}
