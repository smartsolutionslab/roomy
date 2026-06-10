using SmartSolutionsLab.Roomy.Application.Contracts.Messaging;
using SmartSolutionsLab.Roomy.Organization.Domain.Employees;
using SmartSolutionsLab.Roomy.SharedKernel.Results;

namespace SmartSolutionsLab.Roomy.Organization.Application.Commands.Handlers;

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
