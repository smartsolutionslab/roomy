using SmartSolutionsLab.Roomy.Application.Contracts.Messaging;
using SmartSolutionsLab.Roomy.Contracts.Identity;
using SmartSolutionsLab.Roomy.Organization.Application.Commands;
using SmartSolutionsLab.Roomy.Organization.Domain.Employees;

namespace SmartSolutionsLab.Roomy.Organization.Infrastructure.Messaging;

public sealed class UserRegisteredConsumer(ICommandHandler<CompleteEmployeeProvisioning> complete)
{
    public async Task Handle(UserRegistered message, CancellationToken cancellationToken)
    {
        var command = new CompleteEmployeeProvisioning(EmployeeIdentifier.From(message.EmployeeId));
        var result = await complete.HandleAsync(command, cancellationToken);
        if (result.IsFailure)
        {
            // Surface the failure so Wolverine retries and, if it persists, dead-letters it (ADR-0060) —
            // a swallowed result would silently abandon the provisioning saga.
            throw new InvalidOperationException(
                $"Completing provisioning for employee {message.EmployeeId} failed: {result.Error.Code} — {result.Error.Message}");
        }
    }
}
