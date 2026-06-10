using SmartSolutionsLab.Roomy.Application.Contracts.Messaging;

namespace SmartSolutionsLab.Roomy.Attendance.Application.UseCases;

// Intent to list the employees an administrator may act on behalf of (009, AT-6). Administrator-only —
// the endpoint enforces the realm role; the single-tenant company scopes the directory, so no params.
public sealed record ViewEmployees() : IQuery<IReadOnlyList<EmployeeView>>;
