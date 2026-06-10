using SmartSolutionsLab.Roomy.Application.Contracts.Messaging;
using SmartSolutionsLab.Roomy.Contracts.Identity;
using SmartSolutionsLab.Roomy.Organization.Application.Commands;
using SmartSolutionsLab.Roomy.Organization.Domain.Employees;

namespace SmartSolutionsLab.Roomy.Organization.Infrastructure.Messaging;

// The messaging edge of the provisioning saga's failure ack (ADR-0025/0031, 008): Wolverine delivers
// identity's UserProvisioningFailed through the durable inbox, and this consumer maps that foreign
// contract — including its coarse reason — onto the organization-owned FailEmployeeProvisioning command,
// the compensation that marks the employee Failed so no half-account remains (FR-007). This is the only
// place UserProvisioningFailed is referenced; the application never sees identity's published language.
public sealed class UserProvisioningFailedConsumer(ICommandHandler<FailEmployeeProvisioning> fail)
{
    public async Task Handle(UserProvisioningFailed message, CancellationToken cancellationToken)
    {
        var command = new FailEmployeeProvisioning(
            EmployeeIdentifier.From(message.EmployeeId),
            ToReason(message.Reason));

        await fail.HandleAsync(command, cancellationToken);
    }

    private static ProvisioningFailureReason ToReason(UserProvisioningFailureReason reason) => reason switch
    {
        UserProvisioningFailureReason.EmailTaken => ProvisioningFailureReason.EmailTaken,
        UserProvisioningFailureReason.PasswordRejected => ProvisioningFailureReason.PasswordRejected,
        _ => ProvisioningFailureReason.ProviderError,
    };
}
