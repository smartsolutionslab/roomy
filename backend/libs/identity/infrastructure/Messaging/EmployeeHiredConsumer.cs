using SmartSolutionsLab.Roomy.Application.Contracts.Messaging;
using SmartSolutionsLab.Roomy.Contracts.Organization;
using SmartSolutionsLab.Roomy.Identity.Application.Commands;
using SmartSolutionsLab.Roomy.Identity.Domain.Users;

namespace SmartSolutionsLab.Roomy.Identity.Infrastructure.Messaging;

public sealed class EmployeeHiredConsumer(ICommandHandler<RegisterUser> registerUser)
{
    public async Task Handle(EmployeeHired message, CancellationToken cancellationToken)
    {
        var role = message.Role == HiredRole.Administrator
            ? Role.Employee.GrantAdministrator()
            : Role.Employee;

        var command = new RegisterUser(
            UserIdentifier.From(message.UserId),
            message.EmployeeId,
            Email.From(message.Email),
            DisplayName.From(message.DisplayName),
            role,
            message.InitialPassword);

        var result = await registerUser.HandleAsync(command, cancellationToken);
        if (result.IsFailure)
        {
            // A failed registration here is transient (a terminal failure is compensated inside the
            // handler and reported as success). Surface it so the message is retried rather than lost.
            throw new InvalidOperationException(
                $"User provisioning failed for employee {message.EmployeeId}: {result.Error.Code} — {result.Error.Message}");
        }
    }
}
