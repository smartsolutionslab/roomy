using Shouldly;
using SmartSolutionsLab.Roomy.Attendance.Domain.AttendanceDays;
using SmartSolutionsLab.Roomy.Attendance.Domain.AttendanceDays.Events;
using SmartSolutionsLab.Roomy.TestSupport;

namespace SmartSolutionsLab.Roomy.Attendance.Tests.Domain;

public class AttendanceDayCancelTests
{
    private static readonly BookingDate today = BookingDate.From(BookingDates.FirstMondayOnOrAfter(new DateOnly(2026, 6, 1)));
    private static readonly DateTimeOffset occurredAt = new(2026, 6, 8, 9, 0, 0, TimeSpan.Zero);
    private static readonly CompanyIdentifier company = CompanyIdentifier.New();

    [Fact]
    public void Cancelling_a_reservation_raises_reservation_cancelled_and_removes_it()
    {
        var (day, reservationId, _, _) = DayWithReservation(today);

        var result = day.Cancel(reservationId, today, occurredAt);

        result.IsSuccess.ShouldBeTrue();
        day.UncommittedEvents.Count.ShouldBe(1);
        day.UncommittedEvents[0].ShouldBeOfType<ReservationCancelled>().ReservationId.ShouldBe(reservationId.Value);
        day.Reservations.ShouldBeEmpty();
    }

    [Fact]
    public void A_cancelled_place_can_be_reserved_again()
    {
        var (day, reservationId, _, room) = DayWithReservation(today);
        day.Cancel(reservationId, today, occurredAt);

        var reserveAgain = day.Reserve(EmployeeIdentifier.New(), room, RoomCapacity.From(1), today, occurredAt);

        reserveAgain.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public void Reserving_and_cancelling_on_the_same_day_is_allowed()
    {
        var day = AttendanceDay.For(company, today);
        var employee = EmployeeIdentifier.New();
        var placed = day.Reserve(employee, SomeRoom(), RoomCapacity.From(8), today, occurredAt);

        var result = day.Cancel(placed.Value, today, occurredAt);

        result.IsSuccess.ShouldBeTrue();
        day.Reservations.ShouldBeEmpty();
    }

    [Fact]
    public void Cancelling_a_past_day_is_rejected()
    {
        var pastDay = BookingDate.From(today.Value.AddDays(-3));
        var (day, reservationId, _, _) = DayWithReservation(pastDay);

        var result = day.Cancel(reservationId, today, occurredAt);

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("past_immutable");
        day.UncommittedEvents.ShouldBeEmpty();
    }

    [Fact]
    public void Cancelling_an_unknown_reservation_is_not_found()
    {
        var day = AttendanceDay.For(company, today);

        var result = day.Cancel(ReservationIdentifier.New(), today, occurredAt);

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("reservation_not_found");
    }

    private static (AttendanceDay Day, ReservationIdentifier ReservationId, EmployeeIdentifier Owner, RoomReference Room)
        DayWithReservation(BookingDate date)
    {
        var reservationId = ReservationIdentifier.New();
        var owner = EmployeeIdentifier.New();
        var room = SomeRoom();
        var day = AttendanceDay.For(company, date);
        day.LoadFromHistory(
        [
            new ReservationPlaced(reservationId.Value, company.Value, date.Value, owner.Value, room.Office.Value, room.Room.Value, occurredAt),
        ]);
        return (day, reservationId, owner, room);
    }

    private static RoomReference SomeRoom() => RoomReference.From(OfficeIdentifier.New(), RoomIdentifier.New());
}
