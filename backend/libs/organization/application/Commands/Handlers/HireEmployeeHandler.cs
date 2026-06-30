using SmartSolutionsLab.Roomy.Application.Contracts.Messaging;
using SmartSolutionsLab.Roomy.Organization.Domain.Companies;
using SmartSolutionsLab.Roomy.Organization.Domain.Employees;
using SmartSolutionsLab.Roomy.SharedKernel.Results;

namespace SmartSolutionsLab.Roomy.Organization.Application.Commands.Handlers;

public sealed class HireEmployeeHandler(
    ICompanyRepository companies,
    IEmployeeRepository employees,
    IInitialCredentialEncryptor credentialEncryptor,
    IUnitOfWork unitOfWork)
    : ICommandHandler<HireEmployee, HiredEmployee>
{
    public async Task<Result<HiredEmployee>> HandleAsync(HireEmployee command, CancellationToken cancellationToken)
    {
        var (name, email, role, initialPassword) = command;
        var company = await companies.GetSeededAsync(cancellationToken);
        if (company.IsFailure) return company.Error;

        var user = UserIdentifier.New();
        var employee = Employee.Hire(company.Value.Identifier, user, name, email, role, credentialEncryptor.Encrypt(initialPassword));

        await employees.AddAsync(employee, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new HiredEmployee(employee.Identifier, user);
    }
}
