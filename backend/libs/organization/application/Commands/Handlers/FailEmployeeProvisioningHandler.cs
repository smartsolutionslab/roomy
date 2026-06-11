using SmartSolutionsLab.Roomy.Application.Contracts.Messaging;
using SmartSolutionsLab.Roomy.Organization.Domain.Employees;
using SmartSolutionsLab.Roomy.SharedKernel.Results;

namespace SmartSolutionsLab.Roomy.Organization.Application.Commands.Handlers;

public sealed class FailEmployeeProvisioningHandler(IEmployeeRepository employees, IUnitOfWork unitOfWork)
    : ICommandHandler<FailEmployeeProvisioning>
{
    public async Task<Result> HandleAsync(FailEmployeeProvisioning command, CancellationToken cancellationToken)
    {
        var (employeeIdentifier, reason) = command;
        var employee = await employees.GetByIdentifierAsync(employeeIdentifier, cancellationToken);
        if (employee.IsFailure) return employee.Error;

        var transition = employee.Value.FailProvisioning(reason);
        if (transition.IsFailure) return transition.Error;

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
