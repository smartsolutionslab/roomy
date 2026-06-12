using SmartSolutionsLab.Roomy.Application.Contracts.Messaging;
using SmartSolutionsLab.Roomy.Organization.Application.Commands;
using SmartSolutionsLab.Roomy.Organization.Domain.Employees;

namespace SmartSolutionsLab.Roomy.Organization.Api.Seeding;

// Bootstraps the seeded administrator as a first-class employee. Organization is the single
// account-creation entry point (ADR-0025/ADR-0059): hiring the admin here drives the existing saga,
// which provisions the identity User (admin role) + Keycloak and the attendance directory row, so an
// administrator can reserve and view their own reservations. Converges on a usable admin — hires when
// absent, and re-drives provisioning on each startup when the admin exists but is not yet active (a
// no-op once active), so a stuck admin recovers rather than being locked out (ADR-0025).
public sealed class DefaultAdminSeeder(
    IEmployeeRepository employees,
    ICommandHandler<HireEmployee, HiredEmployee> hireEmployee,
    ICommandHandler<RetryEmployeeProvisioning> retryProvisioning,
    DefaultAdminOptions options)
{
    public async Task SeedAsync(CancellationToken cancellationToken)
    {
        var email = WorkEmail.From(options.Email);

        if (await employees.ExistsByWorkEmailAsync(email, cancellationToken))
        {
            var retry = await retryProvisioning.HandleAsync(new RetryEmployeeProvisioning(email, options.InitialPassword), cancellationToken);
            if (retry.IsFailure)
            {
                throw new InvalidOperationException($"DefaultAdmin re-provisioning failed: {retry.Error.Code} — {retry.Error.Message}");
            }

            return;
        }

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
