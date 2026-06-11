using System.Text.Json.Serialization;

namespace SmartSolutionsLab.Roomy.Attendance.Infrastructure.ReadModels.Employees;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
internal sealed record EmployeeSearchCursor(
    [property: JsonRequired] double Similarity,
    string Name,
    Guid EmployeeId);
