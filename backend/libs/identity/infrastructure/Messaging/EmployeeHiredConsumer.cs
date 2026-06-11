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

        await registerUser.HandleAsync(command, cancellationToken);
    }
}
