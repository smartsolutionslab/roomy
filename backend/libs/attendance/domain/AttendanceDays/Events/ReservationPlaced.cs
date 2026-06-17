namespace SmartSolutionsLab.Roomy.Attendance.Domain.AttendanceDays.Events;

public sealed record ReservationPlaced(
    Guid ReservationId,
    Guid CompanyId,
    DateOnly Date,
    Guid EmployeeId,
    Guid OfficeId,
    Guid RoomId,
    DateTimeOffset OccurredAt);
