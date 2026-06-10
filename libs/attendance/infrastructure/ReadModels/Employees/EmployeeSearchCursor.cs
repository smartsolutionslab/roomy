using System.Text.Json.Serialization;

namespace SmartSolutionsLab.Roomy.Attendance.Infrastructure.ReadModels.Employees;

// The opaque cursor for a name search: the (similarity, name, id) of the last returned employee (ADR-0047).
// Similarity is the primary descending key; (name, id) breaks the frequent similarity ties into a stable total
// order. Similarity is required and unmapped members are disallowed, so an unfiltered cursor replayed with a
// query (no similarity) — or any cursor of the wrong shape — fails to decode and is rejected as a malformed
// cursor (ADR-0044 path).
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
internal sealed record EmployeeSearchCursor(
    [property: JsonRequired] double Similarity,
    string Name,
    Guid EmployeeId);
