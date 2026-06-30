using SmartSolutionsLab.Roomy.Application.Contracts.Messaging;
using SmartSolutionsLab.Roomy.Contracts.Organization;
using SmartSolutionsLab.Roomy.Identity.Application.Commands;
using SmartSolutionsLab.Roomy.Identity.Domain.Users;
using SmartSolutionsLab.Roomy.Infrastructure.Cryptography;

namespace SmartSolutionsLab.Roomy.Identity.Infrastructure.Messaging;

public sealed class EmployeeHiredConsumer(ICommandHandler<RegisterUser> registerUser, ICredentialCipher credentialCipher)
{
    public async Task Handle(EmployeeHired message, CancellationToken cancellationToken)
    {
        var (employeeId, userId, email, displayName, hiredRole, encryptedInitialPassword, _) = message;
        var role = Role.From(isAdministrator: hiredRole == HiredRole.Administrator);

        var command = new RegisterUser(
            UserIdentifier.From(userId),
            employeeId, Email.From(email),
            DisplayName.From(displayName),
            role,
            credentialCipher.Decrypt(encryptedInitialPassword));

        var result = await registerUser.HandleAsync(command, cancellationToken);
        if (result.IsFailure)
        {
            // A failed registration here is transient (a terminal failure is compensated inside the
            // handler and reported as success). Surface it so the message is retried rather than lost.
            throw new InvalidOperationException($"User provisioning failed for employee {employeeId}: {result.Error.Code} — {result.Error.Message}");
        }
    }
}
