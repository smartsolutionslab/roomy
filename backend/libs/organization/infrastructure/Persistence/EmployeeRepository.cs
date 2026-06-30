using Microsoft.EntityFrameworkCore;
using SmartSolutionsLab.Roomy.Infrastructure.Persistence.EfCore;
using SmartSolutionsLab.Roomy.Organization.Domain.Employees;
using SmartSolutionsLab.Roomy.SharedKernel.Results;

namespace SmartSolutionsLab.Roomy.Organization.Infrastructure.Persistence;

public sealed class EmployeeRepository(OrganizationDbContext context) : IEmployeeRepository
{
    public Task AddAsync(Employee employee, CancellationToken cancellationToken)
    {
        context.Employees.Add(employee);
        return Task.CompletedTask;
    }

    public Task<Result<Employee>> GetByIdentifierAsync(EmployeeIdentifier identifier, CancellationToken cancellationToken) =>
        context.Employees.SingleOrNotFoundAsync(
            candidate => candidate.Identifier == identifier,
            Error.NotFound("employee.not_found", $"No employee exists with identifier '{identifier}'."),
            cancellationToken);

    public Task<Result<Employee>> GetByWorkEmailAsync(WorkEmail email, CancellationToken cancellationToken) =>
        context.Employees.SingleOrNotFoundAsync(
            candidate => candidate.Email == email,
            Error.NotFound("employee.not_found", $"No employee exists with work email '{email.Value}'."),
            cancellationToken);

    public async Task<bool> ExistsByWorkEmailAsync(WorkEmail email, CancellationToken cancellationToken) =>
        await context.Employees.AnyAsync(candidate => candidate.Email == email, cancellationToken);
}
