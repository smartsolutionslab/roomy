namespace SmartSolutionsLab.Roomy.Attendance.Domain.AttendanceDays;

public sealed record ReservationCancelled(
    Guid ReservationId,
    Guid CompanyId,
    DateOnly Date,
    Guid EmployeeId,
    Guid RoomId,
    DateTimeOffset OccurredAt);
