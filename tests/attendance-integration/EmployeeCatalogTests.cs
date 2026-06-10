using Shouldly;
using SmartSolutionsLab.Roomy.Attendance.Infrastructure.Persistence;
using SmartSolutionsLab.Roomy.Attendance.Infrastructure.ReadModels.Employees;

namespace SmartSolutionsLab.Roomy.Attendance.IntegrationTests;

// The employee-catalog adapter against real PostgreSQL (009): it lists the local Employees read model
// ordered by display name for the administrator on-behalf picker.
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
        var employees = await new EmployeeCatalog(query).GetAsync(TestContext.Current.CancellationToken);

        employees.Select(employee => employee.Name).ShouldContain("Ada");
        employees.Select(employee => employee.Name).ShouldContain("Ben");
        employees.First().Name.ShouldBe("Ada");
    }

    private async Task SeedAsync(Action<AttendanceDbContext> seed)
    {
        await using var context = fixture.CreateDbContext();
        seed(context);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
    }
}
