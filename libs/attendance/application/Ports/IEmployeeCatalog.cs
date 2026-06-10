using SmartSolutionsLab.Roomy.Attendance.Application.UseCases;

namespace SmartSolutionsLab.Roomy.Attendance.Application.Ports;

// Lists the employees an administrator may act on behalf of (009), from attendance's own Employees read
// model — never a cross-context join (ADR-0014). A company with no employees yet yields an empty list;
// absence is not an error here.
public interface IEmployeeCatalog
{
    Task<IReadOnlyList<EmployeeView>> GetAsync(CancellationToken cancellationToken);
}
