namespace SmartSolutionsLab.Roomy.Attendance.Infrastructure.ReadModels;

// The opaque cursor for an employee's reservation history: the day of the last returned reservation
// (ADR-0044). One reservation per employee per day makes the day a unique total order, so no tiebreaker.
internal sealed record ReservationCursor(DateOnly Date);
