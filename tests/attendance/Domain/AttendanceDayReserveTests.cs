using Shouldly;
using SmartSolutionsLab.Roomy.Attendance.Domain.AttendanceDays;
using SmartSolutionsLab.Roomy.TestSupport;

namespace SmartSolutionsLab.Roomy.Attendance.Tests.Domain;

// The AttendanceDay aggregate enforces both reservation invariants inside one consistency boundary
// (ADR-0026): no-overbooking per (room, day) and one reservation per employee per day, plus the
// bookable-day rule. Capacity and "today" are handed in (research R3/R4); the aggregate reads no
// master data and no clock. These specs cover scenarios 1–7 (US1) against the aggregate directly.
public class AttendanceDayReserveTests
{
    private static readonly BookingDate today = BookingDate.From(BookingDates.FirstMondayOnOrAfter(new DateOnly(2026, 6, 1)));
    private static readonly DateTimeOffset occurredAt = new(2026, 6, 8, 9, 0, 0, TimeSpan.Zero);
    private static readonly CompanyIdentifier company = CompanyIdentifier.New();

    [Fact]
    public void Reserving_a_place_on_a_bookable_day_succeeds_and_raises_reservation_placed()
    {
        var day = AttendanceDay.For(company, today);
        var room = SomeRoom();
        var employee = EmployeeIdentifier.New();

        var result = day.Reserve(employee, room, RoomCapacity.From(8), today, occurredAt);

        result.IsSuccess.ShouldBeTrue();
        day.UncommittedEvents.Count.ShouldBe(1);
        var placed = day.UncommittedEvents[0].ShouldBeOfType<ReservationPlaced>();
        placed.ReservationId.ShouldBe(result.Value.Value);
        placed.EmployeeId.ShouldBe(employee.Value);
        placed.OfficeId.ShouldBe(room.Office.Value);
        placed.RoomId.ShouldBe(room.Room.Value);
        placed.Date.ShouldBe(today.Value);
    }

    [Fact]
    public void Reserving_today_is_allowed_when_today_is_a_working_day()
    {
        // Scenario 2 — the same-day (spontaneous) reservation; the window's lower bound is inclusive.
        var day = AttendanceDay.For(company, today);

        var result = day.Reserve(EmployeeIdentifier.New(), SomeRoom(), RoomCapacity.From(8), today, occurredAt);

        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public void A_full_room_rejects_further_reservations()
    {
        // Scenario 3 — capacity 1, already taken.
        var day = AttendanceDay.For(company, today);
        var room = SomeRoom();
        day.Reserve(EmployeeIdentifier.New(), room, RoomCapacity.From(1), today, occurredAt);

        var result = day.Reserve(EmployeeIdentifier.New(), room, RoomCapacity.From(1), today, occurredAt);

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("room_full");
        day.UncommittedEvents.Count.ShouldBe(1); // the rejected reservation leaves no record (edge case)
    }

    [Fact]
    public void An_employee_may_hold_only_one_reservation_per_day()
    {
        // Scenario 4 — across all rooms and offices.
        var day = AttendanceDay.For(company, today);
        var employee = EmployeeIdentifier.New();
        day.Reserve(employee, SomeRoom(), RoomCapacity.From(8), today, occurredAt);

        var result = day.Reserve(employee, SomeRoom(), RoomCapacity.From(8), today, occurredAt);

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("already_reserved_today");
        day.UncommittedEvents.Count.ShouldBe(1);
    }

    [Theory]
    [InlineData(-3)] // the previous Friday — a working day, but in the past (scenario 5)
    [InlineData(5)]  // Saturday (scenario 6)
    [InlineData(6)]  // Sunday (scenario 6)
    [InlineData(15)] // a working day just past the 14-day window (scenario 7)
    public void Reserving_an_unbookable_day_is_rejected(int dayOffset)
    {
        var date = BookingDate.From(today.Value.AddDays(dayOffset));
        var day = AttendanceDay.For(company, date);

        var result = day.Reserve(EmployeeIdentifier.New(), SomeRoom(), RoomCapacity.From(8), today, occurredAt);

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("not_bookable");
        day.UncommittedEvents.ShouldBeEmpty();
    }

    [Fact]
    public void Reserve_counts_capacity_against_the_replayed_history()
    {
        // The capacity check must fold the persisted stream, not just in-memory raises: a room already
        // filled by a replayed ReservationPlaced is full.
        var room = SomeRoom();
        var day = AttendanceDay.For(company, today);
        day.LoadFromHistory(
        [
            new ReservationPlaced(Guid.CreateVersion7(), company.Value, today.Value, Guid.CreateVersion7(), room.Office.Value, room.Room.Value, occurredAt),
        ]);

        var result = day.Reserve(EmployeeIdentifier.New(), room, RoomCapacity.From(1), today, occurredAt);

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("room_full");
    }

    private static RoomReference SomeRoom() => RoomReference.From(OfficeIdentifier.New(), RoomIdentifier.New());
}
