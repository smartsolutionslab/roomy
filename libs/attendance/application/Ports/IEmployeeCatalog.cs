using SmartSolutionsLab.Roomy.Attendance.Application.UseCases;
using SmartSolutionsLab.Roomy.SharedKernel.Pagination;
using SmartSolutionsLab.Roomy.SharedKernel.Results;
using SmartSolutionsLab.Roomy.SharedKernel.Search;

namespace SmartSolutionsLab.Roomy.Attendance.Application.Ports;

// Lists a keyset-paginated page of the employees an administrator may act on behalf of (009), from
// attendance's own Employees read model — never a cross-context join (ADR-0014). A blank term lists in the
// existing display-name keyset order; a non-blank term returns only employees whose names are similar to it,
// ranked most-similar first (012, ADR-0047). The employee id breaks ties (names can collide), so either order
// is a stable total order. No employees yields an empty page; a malformed or wrong-mode cursor is a validation
// failure.
public interface IEmployeeCatalog
{
    Task<Result<Page<EmployeeView>>> GetAsync(
        SearchTerm term,
        PageRequest request,
        CancellationToken cancellationToken);
}
