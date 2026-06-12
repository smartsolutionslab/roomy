using Microsoft.EntityFrameworkCore;
using SmartSolutionsLab.Roomy.Organization.Domain.Employees;
using SmartSolutionsLab.Roomy.SharedKernel.Results;

namespace SmartSolutionsLab.Roomy.Organization.Infrastructure.Persistence;

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

    public async Task<Result<Employee>> GetByWorkEmailAsync(WorkEmail email, CancellationToken cancellationToken)
    {
        var employee = await context.Employees.SingleOrDefaultAsync(candidate => candidate.Email == email, cancellationToken);

        if (employee is null) return Error.NotFound("employee.not_found", $"No employee exists with work email '{email.Value}'.");

        return employee;
    }

    public async Task<bool> ExistsByWorkEmailAsync(WorkEmail email, CancellationToken cancellationToken) =>
        await context.Employees.AnyAsync(candidate => candidate.Email == email, cancellationToken);
}
