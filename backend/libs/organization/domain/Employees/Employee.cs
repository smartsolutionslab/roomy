using SmartSolutionsLab.Roomy.Organization.Domain.Companies;
using SmartSolutionsLab.Roomy.SharedKernel;
using SmartSolutionsLab.Roomy.SharedKernel.Results;

namespace SmartSolutionsLab.Roomy.Organization.Domain.Employees;

public sealed class Employee : Aggregate
{
    private Employee(
        EmployeeIdentifier identifier,
        CompanyIdentifier companyIdentifier,
        UserIdentifier userIdentifier,
        EmployeeName name,
        WorkEmail email,
        EmployeeRole role)
    {
        Identifier = identifier;
        CompanyIdentifier = companyIdentifier;
        UserIdentifier = userIdentifier;
        Name = name;
        Email = email;
        Role = role;
        State = ProvisioningState.Provisioning;
    }

    public EmployeeIdentifier Identifier { get; }
    public CompanyIdentifier CompanyIdentifier { get; }
    public UserIdentifier UserIdentifier { get; }
    public EmployeeName Name { get; private set; }
    public WorkEmail Email { get; private set; }
    public EmployeeRole Role { get; private set; }
    public ProvisioningState State { get; private set; }
    public ProvisioningFailureReason? FailureReason { get; private set; }

    public static Employee Hire(
        CompanyIdentifier company,
        UserIdentifier user,
        EmployeeName name,
        WorkEmail email,
        EmployeeRole role,
        string initialPassword)
    {
        var employee = new Employee(EmployeeIdentifier.New(), company, user, name, email, role);
        employee.RaiseDomainEvent(new EmployeeHired(employee.Identifier, company, user, name, email, role, initialPassword));
        return employee;
    }

    public Result CompleteProvisioning()
    {
        if (State == ProvisioningState.Active)
            return Result.Success();

        if (State == ProvisioningState.Failed)
            return Error.Conflict("employee.terminal", "A failed employee cannot be activated.");

        State = ProvisioningState.Active;
        return Result.Success();
    }

    public Result FailProvisioning(ProvisioningFailureReason reason)
    {
        if (State == ProvisioningState.Failed)
            return Result.Success();

        if (State == ProvisioningState.Active)
            return Error.Conflict("employee.terminal", "An active employee cannot be failed.");

        State = ProvisioningState.Failed;
        FailureReason = reason;
        return Result.Success();
    }
}
