using SmartSolutionsLab.Roomy.Attendance.Domain.AttendanceDays;

namespace SmartSolutionsLab.Roomy.Attendance.Application.Queries;

// A reservation as seen by the day view (scenario 11): the place an employee holds in a room of an
// office on the day. Projected from the AttendanceDay stream by the view handler.
public sealed record ReservationView(
    ReservationIdentifier Reservation,
    OfficeIdentifier Office,
    RoomIdentifier Room,
    BookingDate Date,
    EmployeeIdentifier Employee);
