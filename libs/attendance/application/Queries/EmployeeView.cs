using SmartSolutionsLab.Roomy.Attendance.Domain.AttendanceDays;

namespace SmartSolutionsLab.Roomy.Attendance.Application.Queries;

// One employee in the administrator on-behalf directory (009, AT-6): the id an administrator acts for
// and the display name to choose by. Sourced from attendance's own Employees read model, fed by
// EmployeeHired (ADR-0014/0031), never a cross-context join.
public sealed record EmployeeView(EmployeeIdentifier Employee, string Name);
