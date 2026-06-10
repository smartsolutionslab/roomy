using SmartSolutionsLab.Roomy.Application.Contracts.Messaging;
using SmartSolutionsLab.Roomy.Attendance.Domain.AttendanceDays;

namespace SmartSolutionsLab.Roomy.Attendance.Application.Commands;

public sealed record ReservePlace(
    CompanyIdentifier Company,
    EmployeeIdentifier Employee,
    OfficeIdentifier Office,
    RoomIdentifier Room,
    BookingDate Date) : ICommand<ReservationIdentifier>;
