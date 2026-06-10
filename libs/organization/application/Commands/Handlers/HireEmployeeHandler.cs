using SmartSolutionsLab.Roomy.Application.Contracts.Messaging;
using SmartSolutionsLab.Roomy.Organization.Domain.Companies;
using SmartSolutionsLab.Roomy.Organization.Domain.Employees;
using SmartSolutionsLab.Roomy.SharedKernel.Results;

namespace SmartSolutionsLab.Roomy.Organization.Application.UseCases;

// Hires a colleague under the seeded company (ADR-0025): pre-allocate the login identifier (the saga
// correlation key, FR-006), create the employee in Provisioning (which raises EmployeeHired), persist,
// and commit once. The unit-of-work drain publishes EmployeeHired via the outbox (ADR-0037), so the
// event commits atomically with the employee row. Mirrors CreateOfficeHandler.
public sealed class HireEmployeeHandler(
    ICompanyRepository companies,
    IEmployeeRepository employees,
    IUnitOfWork unitOfWork) : ICommandHandler<HireEmployee, HiredEmployee>
{
    public async Task<Result<HiredEmployee>> HandleAsync(
        HireEmployee command,
        CancellationToken cancellationToken)
    {
        var company = await companies.GetSeededAsync(cancellationToken);
        if (company.IsFailure)
            return company.Error;

        var user = UserIdentifier.New();
        var employee = Employee.Hire(
            company.Value.Identifier, user, command.Name, command.Email, command.Role, command.InitialPassword);

        await employees.AddAsync(employee, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new HiredEmployee(employee.Identifier, user);
    }
}
