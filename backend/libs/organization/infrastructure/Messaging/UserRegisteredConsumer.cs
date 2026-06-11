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
        await complete.HandleAsync(command, cancellationToken);
    }
}
