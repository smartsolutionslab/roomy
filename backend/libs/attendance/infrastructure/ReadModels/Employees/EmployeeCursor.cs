using System.Text.Json.Serialization;

namespace SmartSolutionsLab.Roomy.Attendance.Infrastructure.ReadModels.Employees;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
internal sealed record EmployeeCursor(string Name, Guid EmployeeId);
