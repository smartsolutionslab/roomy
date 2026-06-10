using SmartSolutionsLab.Roomy.Application.Contracts.Messaging;
using SmartSolutionsLab.Roomy.Contracts.Identity;
using SmartSolutionsLab.Roomy.Organization.Application.Commands;
using SmartSolutionsLab.Roomy.Organization.Domain.Employees;

namespace SmartSolutionsLab.Roomy.Organization.Infrastructure.Messaging;

// The messaging edge of the provisioning saga's success ack (ADR-0025/0031, 008): Wolverine delivers
// identity's UserRegistered through the durable inbox, and this consumer maps that foreign published
// contract onto the organization-owned CompleteEmployeeProvisioning command before invoking the use case.
// Keeping the mapping here is what lets the application layer stay free of another context's published
// language — this is the only place UserRegistered is referenced. The EmployeeId correlates the saga.
public sealed class UserRegisteredConsumer(ICommandHandler<CompleteEmployeeProvisioning> complete)
{
    public async Task Handle(UserRegistered message, CancellationToken cancellationToken)
    {
        var command = new CompleteEmployeeProvisioning(EmployeeIdentifier.From(message.EmployeeId));
        await complete.HandleAsync(command, cancellationToken);
    }
}
