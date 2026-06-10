using SmartSolutionsLab.Roomy.Application.Contracts.Messaging;
using SmartSolutionsLab.Roomy.Contracts.Organization;
using SmartSolutionsLab.Roomy.Identity.Application.Commands;
using SmartSolutionsLab.Roomy.Identity.Domain.Users;

namespace SmartSolutionsLab.Roomy.Identity.Infrastructure.Messaging;

// The messaging edge of the provisioning saga (ADR-0025/0031): Wolverine delivers organization's
// EmployeeHired through the durable inbox, and this consumer maps that foreign published contract onto
// the identity-owned RegisterUser command before invoking the use case. Keeping the mapping here is
// what lets the application layer stay free of another context's published language — this is the only
// place EmployeeHired is referenced. A business failure is already surfaced by the handler as a
// published UserProvisioningFailed, so the message is acked; only a transport fault throws and retries.
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
