namespace SmartSolutionsLab.Roomy.Attendance.Domain.AttendanceDays.Events;

public sealed record ReservationCancelled(
    Guid ReservationId,
    Guid CompanyId,
    DateOnly Date,
    Guid EmployeeId,
    Guid RoomId,
    DateTimeOffset OccurredAt);
