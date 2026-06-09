namespace SmartSolutionsLab.Roomy.Attendance.Domain.AttendanceDays;

// A reservation was cancelled, freeing its place (FR-008) — a stream event of the AttendanceDay
// aggregate and part of its source of truth (ADR-0012). Internal to the attendance context, it
// carries primitives for clean serialization/versioning (research R5); the aggregate maps them back
// to value objects in Apply. It needs no OfficeId — the freed place is counted per (room, day).
public sealed record ReservationCancelled(
    Guid ReservationId,
    Guid CompanyId,
    DateOnly Date,
    Guid EmployeeId,
    Guid RoomId,
    DateTimeOffset OccurredAt);
