using Shouldly;
using SmartSolutionsLab.Roomy.Attendance.Application.Ports;
using SmartSolutionsLab.Roomy.Attendance.Application.UseCases;
using SmartSolutionsLab.Roomy.Attendance.Domain.AttendanceDays;
using SmartSolutionsLab.Roomy.SharedKernel.Pagination;
using SmartSolutionsLab.Roomy.SharedKernel.Results;

namespace SmartSolutionsLab.Roomy.Attendance.Tests.Application;

// The on-behalf employee directory use case is a straight read of the catalog (009): the handler returns
// exactly what the port holds, wrapped in a success Result. The SQL (ordering) is covered by the
// read-model integration tests.
public class ViewEmployeesTests
{
    [Fact]
    public async Task It_returns_the_catalogs_employees()
    {
        var employee = new EmployeeView(EmployeeIdentifier.New(), "Ada");
        var handler = new ViewEmployeesHandler(new StubCatalog([employee]));

        var result = await handler.HandleAsync(new ViewEmployees(FirstPage), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Items.ShouldHaveSingleItem().ShouldBe(employee);
    }

    [Fact]
    public async Task No_employees_yields_an_empty_page()
    {
        var handler = new ViewEmployeesHandler(new StubCatalog([]));

        var result = await handler.HandleAsync(new ViewEmployees(FirstPage), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Items.ShouldBeEmpty();
    }

    private static PageRequest FirstPage => PageRequest.From(cursor: null, limit: null).Value;

    private sealed class StubCatalog(IReadOnlyList<EmployeeView> employees) : IEmployeeCatalog
    {
        public Task<Result<Page<EmployeeView>>> GetAsync(PageRequest request, CancellationToken cancellationToken) =>
            Task.FromResult<Result<Page<EmployeeView>>>(new Page<EmployeeView>(employees, null));
    }
}
