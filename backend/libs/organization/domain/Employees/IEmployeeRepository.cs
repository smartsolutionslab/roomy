using SmartSolutionsLab.Roomy.SharedKernel.Results;

namespace SmartSolutionsLab.Roomy.Organization.Domain.Employees;

public interface IEmployeeRepository
{
    Task AddAsync(Employee employee, CancellationToken cancellationToken);

    Task<Result<Employee>> GetByIdentifierAsync(EmployeeIdentifier identifier, CancellationToken cancellationToken);

    Task<Result<Employee>> GetByWorkEmailAsync(WorkEmail email, CancellationToken cancellationToken);

    Task<bool> ExistsByWorkEmailAsync(WorkEmail email, CancellationToken cancellationToken);
}
