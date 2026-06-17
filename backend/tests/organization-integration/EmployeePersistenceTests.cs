using Microsoft.EntityFrameworkCore;
using Shouldly;
using SmartSolutionsLab.Roomy.Organization.Domain.Companies;
using SmartSolutionsLab.Roomy.Organization.Domain.Employees;
using SmartSolutionsLab.Roomy.Organization.Infrastructure.Persistence;
using SmartSolutionsLab.Roomy.SharedKernel.Results;

namespace SmartSolutionsLab.Roomy.Organization.IntegrationTests;

public sealed class EmployeePersistenceTests(PostgresDatabaseFixture fixture)
    : IClassFixture<PostgresDatabaseFixture>
{
    private static readonly CompanyIdentifier company = CompanyIdentifier.New();

    [Fact]
    public async Task Round_trips_a_failed_employee_with_its_state_and_reason()
    {
        var employee = Hire(UserIdentifier.New());
        employee.FailProvisioning(ProvisioningFailureReason.EmailTaken);
        await PersistAsync(employee);

        await using var context = fixture.CreateContext();
        var found = await new EmployeeRepository(context).GetByIdentifierAsync(employee.Identifier, TestContext.Current.CancellationToken);

        found.IsSuccess.ShouldBeTrue();
        found.Value.Name.ShouldBe(EmployeeName.From("Ada Lovelace"));
        found.Value.Email.ShouldBe(WorkEmail.From("ada@example.com"));
        found.Value.Role.ShouldBe(EmployeeRole.Employee);
        found.Value.State.ShouldBe(ProvisioningState.Failed);
        found.Value.FailureReason.ShouldBe(ProvisioningFailureReason.EmailTaken);
    }

    [Fact]
    public async Task GetByIdentifier_returns_NotFound_for_an_unknown_employee()
    {
        await using var context = fixture.CreateContext();
        var found = await new EmployeeRepository(context).GetByIdentifierAsync(EmployeeIdentifier.New(), TestContext.Current.CancellationToken);

        found.IsFailure.ShouldBeTrue();
        found.Error.Type.ShouldBe(ErrorType.NotFound);
    }

    [Fact]
    public async Task Rejects_two_employees_sharing_the_same_user()
    {
        var user = UserIdentifier.New();
        await PersistAsync(Hire(user));

        await Should.ThrowAsync<DbUpdateException>(() => PersistAsync(Hire(user)));
    }

    private async Task PersistAsync(Employee employee)
    {
        await using var context = fixture.CreateContext();
        await new EmployeeRepository(context).AddAsync(employee, TestContext.Current.CancellationToken);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
    }

    private static Employee Hire(UserIdentifier user) =>
        Employee.Hire(
            company,
            user,
            EmployeeName.From("Ada Lovelace"),
            WorkEmail.From("ada@example.com"),
            EmployeeRole.Employee,
            "transient-pw");
}
