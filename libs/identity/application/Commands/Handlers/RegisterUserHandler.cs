using SmartSolutionsLab.Roomy.Application.Contracts.Integration;
using SmartSolutionsLab.Roomy.Application.Contracts.Messaging;
using SmartSolutionsLab.Roomy.Contracts.Identity;
using SmartSolutionsLab.Roomy.Identity.Application.Commands;
using SmartSolutionsLab.Roomy.Identity.Domain.Users;
using SmartSolutionsLab.Roomy.SharedKernel.Results;

namespace SmartSolutionsLab.Roomy.Identity.Application.Commands.Handlers;

// Provisions an account for a hired employee (US3 / IA-3). Provider first, then persist: Keycloak owns
// credentials (ADR-0013), so it is the authority for the two business failures — a taken email and a
// rejected password — which it returns as a coarse Result code, never an exception. On success the
// account is persisted Active and UserRegistered is published; on failure nothing is persisted and
// UserProvisioningFailed carries the reason so the saga can compensate (ADR-0025). Both events go
// through the owned publisher, which the Wolverine outbox commits atomically with the write (ADR-0005).
public sealed class RegisterUserHandler(
    IUserRepository users,
    IIdentityProviderPort identityProvider,
    IIntegrationEventPublisher publisher,
    TimeProvider timeProvider) : ICommandHandler<RegisterUser>
{
    public async Task<Result> HandleAsync(RegisterUser command, CancellationToken cancellationToken)
    {
        var provisioning = await identityProvider.ProvisionUserAsync(
            command.Email,
            command.DisplayName,
            command.InitialPassword,
            command.Role,
            cancellationToken);

        if (provisioning.IsFailure)
        {
            await publisher.PublishAsync(
                new UserProvisioningFailed(
                    command.UserIdentifier.Value,
                    command.EmployeeId,
                    ReasonFor(provisioning.Error),
                    timeProvider.GetUtcNow()),
                cancellationToken);

            return provisioning.Error;
        }

        var subject = provisioning.Value;
        var user = User.Register(command.UserIdentifier, command.Email, command.DisplayName, command.Role);
        user.Activate(subject);

        await users.AddAsync(user, cancellationToken);

        await publisher.PublishAsync(
            new UserRegistered(
                command.UserIdentifier.Value,
                command.EmployeeId,
                command.Email.Value,
                command.Role.IsAdministrator ? AccountRole.Administrator : AccountRole.Employee,
                subject.Value,
                timeProvider.GetUtcNow()),
            cancellationToken);

        return Result.Success();
    }

    // The provider's failure codes are the published-language reasons by design (ADR-0031); anything
    // unrecognised collapses to the catch-all rather than leaking provider detail.
    private static UserProvisioningFailureReason ReasonFor(Error error) => error.Code switch
    {
        "email_taken" => UserProvisioningFailureReason.EmailTaken,
        "password_rejected" => UserProvisioningFailureReason.PasswordRejected,
        _ => UserProvisioningFailureReason.ProviderError,
    };
}
