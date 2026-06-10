using SmartSolutionsLab.Roomy.Application.Contracts.Messaging;
using SmartSolutionsLab.Roomy.Attendance.Application.Ports;
using SmartSolutionsLab.Roomy.SharedKernel.Pagination;
using SmartSolutionsLab.Roomy.SharedKernel.Results;

namespace SmartSolutionsLab.Roomy.Attendance.Application.UseCases;

// Lists a page of the on-behalf employee directory (009, ADR-0042): a straight read of the catalog
// through the port. There is nothing to decide — an empty page is not "not found" — so the handler
// returns what the read model holds. The SQL (keyset, ordering, projection) is covered by its tests.
public sealed class ViewEmployeesHandler(IEmployeeCatalog catalog)
    : IQueryHandler<ViewEmployees, Page<EmployeeView>>
{
    public Task<Result<Page<EmployeeView>>> HandleAsync(
        ViewEmployees query,
        CancellationToken cancellationToken) =>
        catalog.GetAsync(query.Page, cancellationToken);
}
