using Shouldly;
using SmartSolutionsLab.Roomy.Attendance.Application.UseCases;
using SmartSolutionsLab.Roomy.Attendance.Infrastructure.Persistence;
using SmartSolutionsLab.Roomy.Attendance.Infrastructure.ReadModels.Employees;
using SmartSolutionsLab.Roomy.SharedKernel.Pagination;

namespace SmartSolutionsLab.Roomy.Attendance.IntegrationTests;

// The employee-catalog adapter against real PostgreSQL (009): it lists the local Employees read model
// ordered by display name for the administrator on-behalf picker, keyset-paginated by
// (display_name, employee_id) so duplicate names still page deterministically (ADR-0044).
public sealed class EmployeeCatalogTests(PostgresEventStoreFixture fixture)
    : IClassFixture<PostgresEventStoreFixture>
{
    [Fact]
    public async Task It_lists_employees_ordered_by_display_name()
    {
        await SeedAsync(seed =>
        {
            seed.Employees.Add(new Employee { EmployeeId = Guid.CreateVersion7(), UserId = Guid.CreateVersion7(), DisplayName = "Ben" });
            seed.Employees.Add(new Employee { EmployeeId = Guid.CreateVersion7(), UserId = Guid.CreateVersion7(), DisplayName = "Ada" });
        });

        await using var query = fixture.CreateDbContext();
        var page = await new EmployeeCatalog(query).GetAsync(FirstPage, TestContext.Current.CancellationToken);

        var employees = page.Value.Items;
        employees.Select(employee => employee.Name).ShouldContain("Ada");
        employees.Select(employee => employee.Name).ShouldContain("Ben");
        employees.First().Name.ShouldBe("Ada");
    }

    [Fact]
    public async Task It_keyset_paginates_through_duplicate_names_without_skips_or_duplicates()
    {
        // Two employees share a name and one sorts after — the (name, id) keyset must page through them
        // deterministically. Unique names keep the assertion isolated from the shared class fixture, so
        // a full walk filtered to these three ids exercises the tiebreaker without other rows distorting it.
        var sharedName = $"page-{Guid.NewGuid():N}";
        var laterName = $"{sharedName}-z";
        var seededIds = new HashSet<Guid> { Guid.CreateVersion7(), Guid.CreateVersion7(), Guid.CreateVersion7() };
        var ids = seededIds.ToArray();
        await SeedAsync(seed =>
        {
            seed.Employees.Add(new Employee { EmployeeId = ids[0], UserId = Guid.CreateVersion7(), DisplayName = sharedName });
            seed.Employees.Add(new Employee { EmployeeId = ids[1], UserId = Guid.CreateVersion7(), DisplayName = sharedName });
            seed.Employees.Add(new Employee { EmployeeId = ids[2], UserId = Guid.CreateVersion7(), DisplayName = laterName });
        });

        var catalog = new EmployeeCatalog(fixture.CreateDbContext());
        var mine = new List<EmployeeView>();
        string? cursor = null;
        var guard = 0;
        do
        {
            var page = await catalog.GetAsync(Page(limit: 2, cursor), TestContext.Current.CancellationToken);
            if (page.Value.NextCursor is not null)
            {
                page.Value.Items.Count.ShouldBe(2);
            }

            mine.AddRange(page.Value.Items.Where(employee => seededIds.Contains(employee.Employee.Value)));
            cursor = page.Value.NextCursor;
            guard++;
        }
        while (cursor is not null && guard < 1000);

        mine.Select(employee => employee.Employee.Value).ShouldBeUnique();
        mine.Count.ShouldBe(3);
        mine.Take(2).ShouldAllBe(employee => employee.Name == sharedName);
        mine[^1].Name.ShouldBe(laterName);
    }

    [Fact]
    public async Task A_malformed_cursor_is_a_validation_failure()
    {
        await using var query = fixture.CreateDbContext();
        var page = await new EmployeeCatalog(query)
            .GetAsync(Page(limit: 2, cursor: "not-a-cursor"), TestContext.Current.CancellationToken);

        page.IsFailure.ShouldBeTrue();
    }

    private static PageRequest FirstPage => PageRequest.From(cursor: null, limit: null).Value;

    private static PageRequest Page(int limit, string? cursor = null) =>
        PageRequest.From(cursor, limit).Value;

    private async Task SeedAsync(Action<AttendanceDbContext> seed)
    {
        await using var context = fixture.CreateDbContext();
        seed(context);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
    }
}
