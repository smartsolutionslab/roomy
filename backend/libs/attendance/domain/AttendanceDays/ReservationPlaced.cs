namespace SmartSolutionsLab.Roomy.Attendance.Domain.AttendanceDays;

public sealed record ReservationPlaced(
    Guid ReservationId,
    Guid CompanyId,
    DateOnly Date,
    Guid EmployeeId,
    Guid OfficeId,
    Guid RoomId,
    DateTimeOffset OccurredAt);
