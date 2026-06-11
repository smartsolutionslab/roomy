using SmartSolutionsLab.Roomy.Attendance.Domain.AttendanceDays;

namespace SmartSolutionsLab.Roomy.Attendance.Application.Queries;

public sealed record MyReservationView(
    ReservationIdentifier Reservation,
    OfficeIdentifier Office,
    string OfficeName,
    RoomIdentifier Room,
    string RoomName,
    BookingDate Date);
