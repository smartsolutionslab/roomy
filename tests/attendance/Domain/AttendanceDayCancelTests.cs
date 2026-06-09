using Shouldly;
using SmartSolutionsLab.Roomy.Attendance.Domain.AttendanceDays;

namespace SmartSolutionsLab.Roomy.Attendance.Tests.Domain;

// Cancelling a reservation (FR-008/009, scenarios 8–11). Cancel frees the place (a full room becomes
// bookable again), refuses a past day (the past is immutable), and is allowed only for the owner or an
// administrator. Like Reserve, the aggregate is handed "today" and the timestamp — no clock.
public class AttendanceDayCancelTests
{
    private static readonly BookingDate today = BookingDate.From(FirstMondayOnOrAfter(new DateOnly(2026, 6, 1)));
    private static readonly DateTimeOffset occurredAt = new(2026, 6, 8, 9, 0, 0, TimeSpan.Zero);
    private static readonly CompanyIdentifier company = CompanyIdentifier.New();

    [Fact]
    public void Cancelling_a_reservation_raises_reservation_cancelled_and_removes_it()
    {
        var (day, reservationId, owner, _) = DayWithReservation(today);

        var result = day.Cancel(reservationId, owner, actorIsAdmin: false, today, occurredAt);

        result.IsSuccess.ShouldBeTrue();
        day.UncommittedEvents.Count.ShouldBe(1);
        day.UncommittedEvents[0].ShouldBeOfType<ReservationCancelled>().ReservationId.ShouldBe(reservationId.Value);
        day.Reservations.ShouldBeEmpty();
    }

    [Fact]
    public void A_cancelled_place_can_be_reserved_again()
    {
        // Scenario 9 — a full (capacity-1) room frees up when its reservation is cancelled.
        var (day, reservationId, owner, room) = DayWithReservation(today);
        day.Cancel(reservationId, owner, actorIsAdmin: false, today, occurredAt);

        var reserveAgain = day.Reserve(EmployeeIdentifier.New(), room, RoomCapacity.From(1), today, occurredAt);

        reserveAgain.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public void Reserving_and_cancelling_on_the_same_day_is_allowed()
    {
        // Edge case — a same-day reserve then cancel.
        var day = AttendanceDay.For(company, today);
        var employee = EmployeeIdentifier.New();
        var placed = day.Reserve(employee, SomeRoom(), RoomCapacity.From(8), today, occurredAt);

        var result = day.Cancel(placed.Value, employee, actorIsAdmin: false, today, occurredAt);

        result.IsSuccess.ShouldBeTrue();
        day.Reservations.ShouldBeEmpty();
    }

    [Fact]
    public void Cancelling_a_past_day_is_rejected()
    {
        // FR-009 — the past is immutable. The reservation sits on a day before "today".
        var pastDay = BookingDate.From(today.Value.AddDays(-3));
        var (day, reservationId, owner, _) = DayWithReservation(pastDay);

        var result = day.Cancel(reservationId, owner, actorIsAdmin: false, today, occurredAt);

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("past_immutable");
        day.UncommittedEvents.ShouldBeEmpty();
    }

    [Fact]
    public void Cancelling_an_unknown_reservation_is_not_found()
    {
        var day = AttendanceDay.For(company, today);

        var result = day.Cancel(ReservationIdentifier.New(), EmployeeIdentifier.New(), actorIsAdmin: false, today, occurredAt);

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("reservation_not_found");
    }

    [Fact]
    public void A_non_owner_cannot_cancel_anothers_reservation()
    {
        // FR-012 (scenario 11) — an employee may not cancel another's reservation.
        var (day, reservationId, _, _) = DayWithReservation(today);

        var result = day.Cancel(reservationId, EmployeeIdentifier.New(), actorIsAdmin: false, today, occurredAt);

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("not_owner");
        day.UncommittedEvents.ShouldBeEmpty();
    }

    [Fact]
    public void An_administrator_can_cancel_on_behalf_of_anyone()
    {
        // Scenario 10 — an administrator acts on behalf of an employee.
        var (day, reservationId, _, _) = DayWithReservation(today);

        var result = day.Cancel(reservationId, EmployeeIdentifier.New(), actorIsAdmin: true, today, occurredAt);

        result.IsSuccess.ShouldBeTrue();
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
