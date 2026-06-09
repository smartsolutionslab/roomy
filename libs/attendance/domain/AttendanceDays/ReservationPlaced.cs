namespace SmartSolutionsLab.Roomy.Attendance.Domain.AttendanceDays;

// A place was reserved — a stream event of the AttendanceDay aggregate and part of its source of
// truth (ADR-0012). Internal to the attendance context (not an integration event), it carries
// primitives so it serializes and versions cleanly in the event store (research R5); the aggregate
// maps them back to value objects in Apply. OccurredAt is supplied by the caller, never an ambient
// clock.
public sealed record ReservationPlaced(
    Guid ReservationId,
    Guid CompanyId,
    DateOnly Date,
    Guid EmployeeId,
    Guid OfficeId,
    Guid RoomId,
    DateTimeOffset OccurredAt);
