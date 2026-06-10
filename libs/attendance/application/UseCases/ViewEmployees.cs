using SmartSolutionsLab.Roomy.Application.Contracts.Messaging;
using SmartSolutionsLab.Roomy.SharedKernel.Pagination;

namespace SmartSolutionsLab.Roomy.Attendance.Application.UseCases;

// Intent to list a keyset-paginated page of the employees an administrator may act on behalf of (009,
// AT-6; ADR-0042). Administrator-only — the endpoint enforces the realm role; the single-tenant company
// scopes the directory, ordered by display name for the picker.
public sealed record ViewEmployees(PageRequest Page) : IQuery<Page<EmployeeView>>;
