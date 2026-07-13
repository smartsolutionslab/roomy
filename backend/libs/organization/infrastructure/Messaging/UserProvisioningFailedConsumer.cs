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

        var result = await fail.HandleAsync(command, cancellationToken);
        if (result.IsFailure)
        {
            // Surface the failure so Wolverine retries and, if it persists, dead-letters it (ADR-0060) —
            // a swallowed result would silently abandon the provisioning saga.
            throw new InvalidOperationException(
                $"Failing provisioning for employee {message.EmployeeId} failed: {result.Error.Code} — {result.Error.Message}");
        }
    }

    private static ProvisioningFailureReason ToReason(UserProvisioningFailureReason reason) => reason switch
    {
        UserProvisioningFailureReason.EmailTaken => ProvisioningFailureReason.EmailTaken,
        UserProvisioningFailureReason.PasswordRejected => ProvisioningFailureReason.PasswordRejected,
        _ => ProvisioningFailureReason.ProviderError,
    };
}
