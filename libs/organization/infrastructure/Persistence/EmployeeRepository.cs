using Microsoft.EntityFrameworkCore;
using SmartSolutionsLab.Roomy.Organization.Domain.Employees;
using SmartSolutionsLab.Roomy.SharedKernel.Results;

namespace SmartSolutionsLab.Roomy.Organization.Infrastructure.Persistence;

// Persists and fetches Employee aggregates from the organization database. A fetch that may miss returns
// Error.NotFound (never null), so the ack-handlers handle an unknown employee explicitly.
public sealed class EmployeeRepository(OrganizationDbContext context) : IEmployeeRepository
{
    public async Task AddAsync(Employee employee, CancellationToken cancellationToken) =>
        await context.Employees.AddAsync(employee, cancellationToken);

    public async Task<Result<Employee>> GetByIdentifierAsync(EmployeeIdentifier identifier, CancellationToken cancellationToken)
    {
        var employee = await context.Employees.SingleOrDefaultAsync(candidate => candidate.Identifier == identifier, cancellationToken);

        if (employee is null) return Error.NotFound("employee.not_found", $"No employee exists with identifier '{identifier}'.");

        return employee;
    }
}
