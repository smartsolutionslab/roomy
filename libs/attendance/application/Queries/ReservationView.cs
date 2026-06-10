using SmartSolutionsLab.Roomy.Attendance.Domain.AttendanceDays;

namespace SmartSolutionsLab.Roomy.Attendance.Application.Queries;

public sealed record ReservationView(
    ReservationIdentifier Reservation,
    OfficeIdentifier Office,
    RoomIdentifier Room,
    BookingDate Date,
    EmployeeIdentifier Employee);
