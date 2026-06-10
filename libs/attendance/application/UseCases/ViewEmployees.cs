using SmartSolutionsLab.Roomy.Application.Contracts.Messaging;
using SmartSolutionsLab.Roomy.SharedKernel.Pagination;
using SmartSolutionsLab.Roomy.SharedKernel.Search;

namespace SmartSolutionsLab.Roomy.Attendance.Application.UseCases;

// Intent to list a keyset-paginated page of the employees an administrator may act on behalf of (009,
// AT-6; ADR-0044). Administrator-only — the endpoint enforces the realm role; the single-tenant company
// scopes the directory. A blank Term lists in display-name order; a non-blank Term searches by name
// similarity, best match first (012, ADR-0047).
public sealed record ViewEmployees(SearchTerm Term, PageRequest Page) : IQuery<Page<EmployeeView>>;
