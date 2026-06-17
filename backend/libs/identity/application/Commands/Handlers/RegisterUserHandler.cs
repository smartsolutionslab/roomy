using SmartSolutionsLab.Roomy.Application.Contracts.Integration;
using SmartSolutionsLab.Roomy.Application.Contracts.Messaging;
using SmartSolutionsLab.Roomy.Contracts.Identity;
using SmartSolutionsLab.Roomy.Identity.Domain.Users;
using SmartSolutionsLab.Roomy.SharedKernel.Results;

namespace SmartSolutionsLab.Roomy.Identity.Application.Commands.Handlers;

public sealed class RegisterUserHandler(IUserRepository users, IIdentityProviderPort identityProvider, IIntegrationEventPublisher publisher, TimeProvider timeProvider)
    : ICommandHandler<RegisterUser>
{
    public async Task<Result> HandleAsync(RegisterUser command, CancellationToken cancellationToken)
    {
        var (userIdentifier, employeeId, email, displayName, role, initialPassword) = command;

        var provisioning = await identityProvider.ProvisionUserAsync(
            userIdentifier,
            email,
            displayName,
            initialPassword,
            role,
            cancellationToken);

        if (provisioning.IsFailure)
        {
            if (!IsTerminal(provisioning.Error)) return provisioning.Error;

            var integrationEvent = new UserProvisioningFailed(
                userIdentifier.Value,
                employeeId,
                ReasonFor(provisioning.Error),
                timeProvider.GetUtcNow());
            await publisher.PublishAsync(integrationEvent, cancellationToken);

            return Result.Success();
        }

        var subject = provisioning.Value;
        var user = User.Register(userIdentifier, email, displayName, role);
        user.Activate(subject);

        await users.AddAsync(user, cancellationToken);

        var @event = new UserRegistered(
            userIdentifier.Value,
            employeeId,
            email.Value,
            role.IsAdministrator ? AccountRole.Administrator : AccountRole.Employee,
            subject.Value,
            timeProvider.GetUtcNow());
        await publisher.PublishAsync(@event, cancellationToken);

        return Result.Success();
    }

    private static bool IsTerminal(Error error) => error.Code is "email_taken" or "password_rejected";

    private static UserProvisioningFailureReason ReasonFor(Error error) => error.Code switch
    {
        "email_taken" => UserProvisioningFailureReason.EmailTaken,
        "password_rejected" => UserProvisioningFailureReason.PasswordRejected,
        _ => UserProvisioningFailureReason.ProviderError,
    };
}
