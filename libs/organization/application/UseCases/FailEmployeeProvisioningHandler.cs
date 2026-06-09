using SmartSolutionsLab.Roomy.Application.Contracts.Messaging;
using SmartSolutionsLab.Roomy.Organization.Domain.Employees;
using SmartSolutionsLab.Roomy.SharedKernel.Results;

namespace SmartSolutionsLab.Roomy.Organization.Application.UseCases;

// Compensates a failed provisioning (ADR-0025): load the employee and mark it Failed with the reason, so
// no usable half-account remains (FR-007). The transition is idempotent (a re-delivered failure is a
// no-op), so at-least-once delivery is safe (FR-008).
public sealed class FailEmployeeProvisioningHandler(
    IEmployeeRepository employees,
    IUnitOfWork unitOfWork) : ICommandHandler<FailEmployeeProvisioning>
{
    public async Task<Result> HandleAsync(
        FailEmployeeProvisioning command,
        CancellationToken cancellationToken)
    {
        var employee = await employees.GetByIdentifierAsync(command.Employee, cancellationToken);
        if (employee.IsFailure)
            return employee.Error;

        var transition = employee.Value.FailProvisioning(command.Reason);
        if (transition.IsFailure)
            return transition.Error;

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
