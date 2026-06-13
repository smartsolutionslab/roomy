using NSubstitute;
using Shouldly;
using SmartSolutionsLab.Roomy.Attendance.Application.Ports;
using SmartSolutionsLab.Roomy.Attendance.Application.Queries;
using SmartSolutionsLab.Roomy.Attendance.Application.Queries.Handlers;
using SmartSolutionsLab.Roomy.Attendance.Domain.AttendanceDays;
using SmartSolutionsLab.Roomy.SharedKernel.Pagination;
using SmartSolutionsLab.Roomy.SharedKernel.Results;
using SmartSolutionsLab.Roomy.SharedKernel.Search;

namespace SmartSolutionsLab.Roomy.Attendance.Tests.Application;

public class ViewEmployeesTests
{
    [Fact]
    public async Task It_returns_the_catalogs_employees()
    {
        var employee = new EmployeeView(EmployeeIdentifier.New(), "Ada");
        var catalog = Substitute.For<IEmployeeCatalog>();
        catalog.GetAsync(Arg.Any<SearchTerm>(), Arg.Any<PageRequest>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success(new Page<EmployeeView>([employee], null)));
        var handler = new ViewEmployeesHandler(catalog);

        var result = await handler.HandleAsync(new ViewEmployees(new EmployeeFilter(SearchTerm.None, FirstPage)), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Items.ShouldHaveSingleItem().ShouldBe(employee);
    }

    [Fact]
    public async Task No_employees_yields_an_empty_page()
    {
        var catalog = Substitute.For<IEmployeeCatalog>();
        catalog.GetAsync(Arg.Any<SearchTerm>(), Arg.Any<PageRequest>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success(new Page<EmployeeView>([], null)));
        var handler = new ViewEmployeesHandler(catalog);

        var result = await handler.HandleAsync(new ViewEmployees(new EmployeeFilter(SearchTerm.None, FirstPage)), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Items.ShouldBeEmpty();
    }

    [Fact]
    public async Task It_forwards_the_search_term_to_the_catalog()
    {
        var term = SearchTerm.From("hanah").Value;
        var catalog = Substitute.For<IEmployeeCatalog>();
        catalog.GetAsync(Arg.Any<SearchTerm>(), Arg.Any<PageRequest>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success(new Page<EmployeeView>([], null)));
        var handler = new ViewEmployeesHandler(catalog);

        await handler.HandleAsync(new ViewEmployees(new EmployeeFilter(term, FirstPage)), CancellationToken.None);

        await catalog.Received(1).GetAsync(term, Arg.Any<PageRequest>(), Arg.Any<CancellationToken>());
    }

    private static PageRequest FirstPage => PageRequest.From(cursor: null, limit: null);
}
