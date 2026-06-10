using Shouldly;
using SmartSolutionsLab.Roomy.Attendance.Domain.AttendanceDays;
using SmartSolutionsLab.Roomy.TestSupport;

namespace SmartSolutionsLab.Roomy.Attendance.Tests.Domain;

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
        var day = AttendanceDay.For(company, today);

        var result = day.Reserve(EmployeeIdentifier.New(), SomeRoom(), RoomCapacity.From(8), today, occurredAt);

        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public void A_full_room_rejects_further_reservations()
    {
        var day = AttendanceDay.For(company, today);
        var room = SomeRoom();
        day.Reserve(EmployeeIdentifier.New(), room, RoomCapacity.From(1), today, occurredAt);

        var result = day.Reserve(EmployeeIdentifier.New(), room, RoomCapacity.From(1), today, occurredAt);

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("room_full");
        day.UncommittedEvents.Count.ShouldBe(1);
    }

    [Fact]
    public void An_employee_may_hold_only_one_reservation_per_day()
    {
        var day = AttendanceDay.For(company, today);
        var employee = EmployeeIdentifier.New();
        day.Reserve(employee, SomeRoom(), RoomCapacity.From(8), today, occurredAt);

        var result = day.Reserve(employee, SomeRoom(), RoomCapacity.From(8), today, occurredAt);

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("already_reserved_today");
        day.UncommittedEvents.Count.ShouldBe(1);
    }

    [Theory]
    [InlineData(-3)]
    [InlineData(5)]
    [InlineData(6)]
    [InlineData(15)]
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
