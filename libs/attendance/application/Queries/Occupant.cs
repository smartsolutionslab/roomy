using SmartSolutionsLab.Roomy.Attendance.Domain.AttendanceDays;

namespace SmartSolutionsLab.Roomy.Attendance.Application.Queries;

// An employee booked in a room on a day, as named in the occupancy view. Shown only for today and the
// following day (FR-007, data minimisation); on every other day the room reports counts without
// occupants.
public sealed record Occupant(EmployeeIdentifier Employee, string Name);
