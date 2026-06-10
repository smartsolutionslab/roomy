using SmartSolutionsLab.Roomy.Application.Contracts.Messaging;
using SmartSolutionsLab.Roomy.Attendance.Application.Ports;
using SmartSolutionsLab.Roomy.SharedKernel.Results;

namespace SmartSolutionsLab.Roomy.Attendance.Application.UseCases;

// Lists the on-behalf employee directory (009): a straight read of the catalog through the port. There
// is nothing to decide — no employees yields an empty list, never "not found" — so the handler returns
// what the read model holds. The SQL (ordering, projection) is covered by the read-model tests.
public sealed class ViewEmployeesHandler(IEmployeeCatalog catalog)
    : IQueryHandler<ViewEmployees, IReadOnlyList<EmployeeView>>
{
    public async Task<Result<IReadOnlyList<EmployeeView>>> HandleAsync(
        ViewEmployees query,
        CancellationToken cancellationToken)
    {
        var employees = await catalog.GetAsync(cancellationToken).ConfigureAwait(false);
        return Result.Success(employees);
    }
}
