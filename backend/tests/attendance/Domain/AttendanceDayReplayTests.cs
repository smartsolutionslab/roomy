using Shouldly;
using SmartSolutionsLab.Roomy.Attendance.Domain.AttendanceDays;
using SmartSolutionsLab.Roomy.Attendance.Domain.AttendanceDays.Events;
using SmartSolutionsLab.Roomy.TestSupport;

namespace SmartSolutionsLab.Roomy.Attendance.Tests.Domain;

public sealed class AttendanceDayReplayTests
{
    private static readonly BookingDate today = BookingDate.From(BookingDates.FirstMondayOnOrAfter(new DateOnly(2026, 6, 1)));
    private static readonly DateTimeOffset occurredAt = new(2026, 6, 8, 9, 0, 0, TimeSpan.Zero);
    private static readonly CompanyIdentifier company = CompanyIdentifier.New();

    [Fact]
    public void Replaying_an_unrecognised_event_throws_naming_the_event_type()
    {
        var day = AttendanceDay.For(company, today);

        var exception = Should.Throw<ArgumentOutOfRangeException>(() => day.LoadFromHistory([new UnknownEvent()]));

        exception.Message.ShouldContain(nameof(UnknownEvent));
    }

    [Fact]
    public void Replaying_the_known_events_reconstructs_the_reservation_state()
    {
        var room = RoomReference.From(OfficeIdentifier.New(), RoomIdentifier.New());
        var reservationId = Guid.CreateVersion7();
        var employeeId = Guid.CreateVersion7();
        var day = AttendanceDay.For(company, today);

        day.LoadFromHistory(
        [
            new ReservationPlaced(reservationId, company.Value, today.Value, employeeId, room.Office.Value, room.Room.Value, occurredAt),
        ]);

        var reservation = day.Reservations.ShouldHaveSingleItem();
        reservation.Identifier.Value.ShouldBe(reservationId);
    }

    private sealed record UnknownEvent;
}
