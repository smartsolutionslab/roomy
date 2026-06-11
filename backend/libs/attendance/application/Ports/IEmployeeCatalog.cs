using SmartSolutionsLab.Roomy.Attendance.Application.Queries;
using SmartSolutionsLab.Roomy.SharedKernel.Pagination;
using SmartSolutionsLab.Roomy.SharedKernel.Results;
using SmartSolutionsLab.Roomy.SharedKernel.Search;

namespace SmartSolutionsLab.Roomy.Attendance.Application.Ports;

public interface IEmployeeCatalog
{
    Task<Result<Page<EmployeeView>>> GetAsync(
        SearchTerm term,
        PageRequest request,
        CancellationToken cancellationToken);
}
