using SmartSolutionsLab.Roomy.Application.Contracts.Messaging;
using SmartSolutionsLab.Roomy.Organization.Domain.Employees;
using SmartSolutionsLab.Roomy.SharedKernel.Results;

namespace SmartSolutionsLab.Roomy.Organization.Application.Commands.Handlers;

public sealed class RetryEmployeeProvisioningHandler(
    IEmployeeRepository employees,
    IInitialCredentialEncryptor credentialEncryptor,
    IUnitOfWork unitOfWork)
    : ICommandHandler<RetryEmployeeProvisioning>
{
    public async Task<Result> HandleAsync(RetryEmployeeProvisioning command, CancellationToken cancellationToken)
    {
        var (email, initialPassword) = command;
        var employee = await employees.GetByWorkEmailAsync(email, cancellationToken);
        if (employee.IsFailure) return employee.Error;

        var transition = employee.Value.RetryProvisioning(credentialEncryptor.Encrypt(initialPassword));
        if (transition.IsFailure) return transition.Error;

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
