using Shouldly;
using SmartSolutionsLab.Roomy.Organization.Domain.Companies;
using SmartSolutionsLab.Roomy.Organization.Domain.Employees;

namespace SmartSolutionsLab.Roomy.Organization.Tests.Domain.Employees;

// The Employee aggregate is the consistency boundary for the provisioning lifecycle (ADR-0025): Hire
// records it in Provisioning and raises EmployeeHired; the saga acks drive it to the terminal Active or
// Failed states. Terminal states are guarded and re-delivered acks are idempotent no-ops, which is what
// makes "no half-accounts" (FR-007) an enforced invariant rather than a convention.
public sealed class EmployeeTests
{
    private static readonly CompanyIdentifier company = CompanyIdentifier.New();

    [Fact]
    public void Hiring_records_a_provisioning_employee_and_raises_employee_hired()
    {
        var user = UserIdentifier.New();

        var employee = Hire(user);

        employee.State.ShouldBe(ProvisioningState.Provisioning);
        employee.UserIdentifier.ShouldBe(user);
        var hired = employee.DomainEvents.OfType<EmployeeHired>().ShouldHaveSingleItem();
        hired.Employee.ShouldBe(employee.Identifier);
        hired.Company.ShouldBe(company);
        hired.User.ShouldBe(user);
        hired.Name.ShouldBe(employee.Name);
        hired.Email.ShouldBe(employee.Email);
        hired.Role.ShouldBe(EmployeeRole.Employee);
        hired.InitialPassword.ShouldBe("transient-pw");
    }

    [Fact]
    public void Completing_provisioning_activates_the_employee()
    {
        var employee = Hire();

        employee.CompleteProvisioning().IsSuccess.ShouldBeTrue();

        employee.State.ShouldBe(ProvisioningState.Active);
        employee.FailureReason.ShouldBeNull();
    }

    [Fact]
    public void Completing_an_already_active_employee_is_an_idempotent_no_op()
    {
        var employee = Hire();
        employee.CompleteProvisioning();

        employee.CompleteProvisioning().IsSuccess.ShouldBeTrue();

        employee.State.ShouldBe(ProvisioningState.Active);
    }

    [Fact]
    public void Failing_provisioning_marks_the_employee_failed_with_the_reason()
    {
        var employee = Hire();

        employee.FailProvisioning(ProvisioningFailureReason.EmailTaken).IsSuccess.ShouldBeTrue();

        employee.State.ShouldBe(ProvisioningState.Failed);
        employee.FailureReason.ShouldBe(ProvisioningFailureReason.EmailTaken);
    }

    [Fact]
    public void Failing_an_already_failed_employee_is_an_idempotent_no_op()
    {
        var employee = Hire();
        employee.FailProvisioning(ProvisioningFailureReason.ProviderError);

        employee.FailProvisioning(ProvisioningFailureReason.EmailTaken).IsSuccess.ShouldBeTrue();

        employee.FailureReason.ShouldBe(ProvisioningFailureReason.ProviderError);
    }

    [Fact]
    public void An_active_employee_cannot_be_failed()
    {
        var employee = Hire();
        employee.CompleteProvisioning();

        var result = employee.FailProvisioning(ProvisioningFailureReason.EmailTaken);

        result.IsFailure.ShouldBeTrue();
        employee.State.ShouldBe(ProvisioningState.Active);
    }

    [Fact]
    public void A_failed_employee_cannot_be_activated()
    {
        var employee = Hire();
        employee.FailProvisioning(ProvisioningFailureReason.EmailTaken);

        var result = employee.CompleteProvisioning();

        result.IsFailure.ShouldBeTrue();
        employee.State.ShouldBe(ProvisioningState.Failed);
    }

    private static Employee Hire(UserIdentifier? user = null) =>
        Employee.Hire(
            company,
            user ?? UserIdentifier.New(),
            EmployeeName.From("Ada Lovelace"),
            WorkEmail.From("ada@example.com"),
            EmployeeRole.Employee,
            "transient-pw");
}
