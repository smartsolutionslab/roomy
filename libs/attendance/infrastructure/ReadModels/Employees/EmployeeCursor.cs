using System.Text.Json.Serialization;

namespace SmartSolutionsLab.Roomy.Attendance.Infrastructure.ReadModels.Employees;

// The opaque cursor for the unfiltered directory: the (name, id) of the last returned employee (ADR-0044).
// The id breaks ties so duplicate display names still page deterministically. Disallow unmapped members so a
// search cursor (which also carries a similarity) replayed with a blank q fails to decode — a 400, not a
// silent wrong-mode read (ADR-0047 §2).
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
internal sealed record EmployeeCursor(string Name, Guid EmployeeId);
