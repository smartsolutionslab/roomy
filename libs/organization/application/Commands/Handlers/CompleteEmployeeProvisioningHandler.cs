using SmartSolutionsLab.Roomy.Application.Contracts.Messaging;
using SmartSolutionsLab.Roomy.Organization.Domain.Employees;
using SmartSolutionsLab.Roomy.SharedKernel.Results;

namespace SmartSolutionsLab.Roomy.Organization.Application.Commands.Handlers;

// Completes provisioning on the identity ack (ADR-0025): load the employee, transition it Active, and
// commit. The transition is idempotent (a re-delivered UserRegistered is a no-op), so at-least-once
// delivery is safe (FR-008).
public sealed class CompleteEmployeeProvisioningHandler(IEmployeeRepository employees, IUnitOfWork unitOfWork)
    : ICommandHandler<CompleteEmployeeProvisioning>
{
    public async Task<Result> HandleAsync(CompleteEmployeeProvisioning command, CancellationToken cancellationToken)
    {
        var employee = await employees.GetByIdentifierAsync(command.Employee, cancellationToken);
        if (employee.IsFailure) return employee.Error;

        var transition = employee.Value.CompleteProvisioning();
        if (transition.IsFailure) return transition.Error;

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
