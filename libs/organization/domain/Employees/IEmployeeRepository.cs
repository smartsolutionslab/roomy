using SmartSolutionsLab.Roomy.SharedKernel.Results;

namespace SmartSolutionsLab.Roomy.Organization.Domain.Employees;

// Persists and fetches Employee aggregates. A fetch that may miss returns Result<Employee>
// (Error.NotFound) rather than null, so the ack-handlers handle an unknown employee explicitly.
public interface IEmployeeRepository
{
    Task AddAsync(Employee employee, CancellationToken cancellationToken);

    Task<Result<Employee>> GetByIdentifierAsync(EmployeeIdentifier identifier, CancellationToken cancellationToken);
}
