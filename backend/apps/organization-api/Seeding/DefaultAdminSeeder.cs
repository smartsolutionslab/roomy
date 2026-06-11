using SmartSolutionsLab.Roomy.Application.Contracts.Messaging;
using SmartSolutionsLab.Roomy.Organization.Application.Commands;
using SmartSolutionsLab.Roomy.Organization.Domain.Employees;

namespace SmartSolutionsLab.Roomy.Organization.Api.Seeding;

// Bootstraps the seeded administrator as a first-class employee. Organization is the single
// account-creation entry point (ADR-0025/ADR-0059): hiring the admin here drives the existing saga,
// which provisions the identity User (admin role) + Keycloak and the attendance directory row, so an
// administrator can reserve and view their own reservations. Idempotent — a no-op once the admin exists.
public sealed class DefaultAdminSeeder(
    IEmployeeRepository employees,
    ICommandHandler<HireEmployee, HiredEmployee> hireEmployee,
    DefaultAdminOptions options)
{
    public async Task SeedAsync(CancellationToken cancellationToken)
    {
        var email = WorkEmail.From(options.Email);
        if (await employees.ExistsByWorkEmailAsync(email, cancellationToken)) return;

        var command = new HireEmployee(
            EmployeeName.From(options.DisplayName),
            email,
            EmployeeRole.Administrator,
            options.InitialPassword);

        var result = await hireEmployee.HandleAsync(command, cancellationToken);
        if (result.IsFailure)
        {
            throw new InvalidOperationException($"DefaultAdmin seeding failed: {result.Error.Code} — {result.Error.Message}");
        }
    }
}
