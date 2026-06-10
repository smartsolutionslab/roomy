using Shouldly;
using SmartSolutionsLab.Roomy.Attendance.Application.Ports;
using SmartSolutionsLab.Roomy.Attendance.Application.UseCases;
using SmartSolutionsLab.Roomy.Attendance.Domain.AttendanceDays;

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

        var result = await handler.HandleAsync(new ViewEmployees(), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldHaveSingleItem().ShouldBe(employee);
    }

    [Fact]
    public async Task No_employees_yields_an_empty_list()
    {
        var handler = new ViewEmployeesHandler(new StubCatalog([]));

        var result = await handler.HandleAsync(new ViewEmployees(), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBeEmpty();
    }

    private sealed class StubCatalog(IReadOnlyList<EmployeeView> employees) : IEmployeeCatalog
    {
        public Task<IReadOnlyList<EmployeeView>> GetAsync(CancellationToken cancellationToken) =>
            Task.FromResult(employees);
    }
}
