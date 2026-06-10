using SmartSolutionsLab.Roomy.Attendance.Domain.AttendanceDays;

namespace SmartSolutionsLab.Roomy.Attendance.Application.UseCases;

// One of an employee's own reservations, as listed in "my reservations" (FR-004, scenario 6): the place
// they hold in a room of an office on a day. Past, today, and future are all listed; the client offers
// cancellation only for future days (the rule lives in 003).
public sealed record MyReservationView(
    ReservationIdentifier Reservation,
    OfficeIdentifier Office,
    string OfficeName,
    RoomIdentifier Room,
    string RoomName,
    BookingDate Date);
