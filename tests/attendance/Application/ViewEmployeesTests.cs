using Shouldly;
using SmartSolutionsLab.Roomy.Attendance.Application.Ports;
using SmartSolutionsLab.Roomy.Attendance.Application.UseCases;
using SmartSolutionsLab.Roomy.Attendance.Domain.AttendanceDays;
using SmartSolutionsLab.Roomy.SharedKernel.Pagination;
using SmartSolutionsLab.Roomy.SharedKernel.Results;
using SmartSolutionsLab.Roomy.SharedKernel.Search;

namespace SmartSolutionsLab.Roomy.Attendance.Tests.Application;

// The on-behalf employee directory use case is a straight read of the catalog (009, 012): the handler returns
// exactly what the port holds, wrapped in a success Result, and forwards the search term unchanged. The SQL
// (ordering, similarity) is covered by the read-model integration tests.
public class ViewEmployeesTests
{
    [Fact]
    public async Task It_returns_the_catalogs_employees()
    {
        var employee = new EmployeeView(EmployeeIdentifier.New(), "Ada");
        var handler = new ViewEmployeesHandler(new StubCatalog([employee]));

        var result = await handler.HandleAsync(new ViewEmployees(SearchTerm.None, FirstPage), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Items.ShouldHaveSingleItem().ShouldBe(employee);
    }

    [Fact]
    public async Task No_employees_yields_an_empty_page()
    {
        var handler = new ViewEmployeesHandler(new StubCatalog([]));

        var result = await handler.HandleAsync(new ViewEmployees(SearchTerm.None, FirstPage), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Items.ShouldBeEmpty();
    }

    [Fact]
    public async Task It_forwards_the_search_term_to_the_catalog()
    {
        var catalog = new StubCatalog([]);
        var handler = new ViewEmployeesHandler(catalog);
        var term = SearchTerm.From("hanah").Value;

        await handler.HandleAsync(new ViewEmployees(term, FirstPage), CancellationToken.None);

        catalog.LastTerm.ShouldBe(term);
    }

    private static PageRequest FirstPage => PageRequest.From(cursor: null, limit: null).Value;

    private sealed class StubCatalog(IReadOnlyList<EmployeeView> employees) : IEmployeeCatalog
    {
        public SearchTerm? LastTerm { get; private set; }

        public Task<Result<Page<EmployeeView>>> GetAsync(SearchTerm term, PageRequest request, CancellationToken cancellationToken)
        {
            LastTerm = term;
            return Task.FromResult<Result<Page<EmployeeView>>>(new Page<EmployeeView>(employees, null));
        }
    }
}
