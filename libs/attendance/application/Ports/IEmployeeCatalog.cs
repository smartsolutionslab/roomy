using SmartSolutionsLab.Roomy.Attendance.Application.UseCases;
using SmartSolutionsLab.Roomy.SharedKernel.Pagination;
using SmartSolutionsLab.Roomy.SharedKernel.Results;

namespace SmartSolutionsLab.Roomy.Attendance.Application.Ports;

// Lists a keyset-paginated page of the employees an administrator may act on behalf of (009), from
// attendance's own Employees read model — never a cross-context join (ADR-0014). Ordered by display
// name with the employee id as tiebreaker (names can collide), so the cursor is a stable total order.
// No employees yields an empty page; a malformed cursor is a validation failure.
public interface IEmployeeCatalog
{
    Task<Result<Page<EmployeeView>>> GetAsync(PageRequest request, CancellationToken cancellationToken);
}
