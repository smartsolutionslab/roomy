using SmartSolutionsLab.Roomy.Application.Contracts.Messaging;
using SmartSolutionsLab.Roomy.Attendance.Application.Ports;
using SmartSolutionsLab.Roomy.SharedKernel.Pagination;
using SmartSolutionsLab.Roomy.SharedKernel.Results;

namespace SmartSolutionsLab.Roomy.Attendance.Application.Queries.Handlers;

public sealed class ViewEmployeesHandler(IEmployeeCatalog catalog)
    : IQueryHandler<ViewEmployees, Page<EmployeeView>>
{
    public async Task<Result<Page<EmployeeView>>> HandleAsync(ViewEmployees query, CancellationToken cancellationToken) =>
        await catalog.GetAsync(query.Filter.Term, query.Filter.Page, cancellationToken);
}
