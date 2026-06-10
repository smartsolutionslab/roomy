using SmartSolutionsLab.Roomy.Application.Contracts.Messaging;
using SmartSolutionsLab.Roomy.Contracts.Identity;
using SmartSolutionsLab.Roomy.Organization.Application.Commands;
using SmartSolutionsLab.Roomy.Organization.Domain.Employees;

namespace SmartSolutionsLab.Roomy.Organization.Infrastructure.Messaging;

public sealed class UserProvisioningFailedConsumer(ICommandHandler<FailEmployeeProvisioning> fail)
{
    public async Task Handle(UserProvisioningFailed message, CancellationToken cancellationToken)
    {
        var employee = EmployeeIdentifier.From(message.EmployeeId);
        var reason = ToReason(message.Reason);

        var command = new FailEmployeeProvisioning(employee, reason);

        await fail.HandleAsync(command, cancellationToken);
    }

    private static ProvisioningFailureReason ToReason(UserProvisioningFailureReason reason) => reason switch
    {
        UserProvisioningFailureReason.EmailTaken => ProvisioningFailureReason.EmailTaken,
        UserProvisioningFailureReason.PasswordRejected => ProvisioningFailureReason.PasswordRejected,
        _ => ProvisioningFailureReason.ProviderError,
    };
}
