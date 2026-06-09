using SmartSolutionsLab.Roomy.Application.Contracts.Messaging;
using SmartSolutionsLab.Roomy.Identity.Domain.Users;
using SmartSolutionsLab.Roomy.SharedKernel.Results;

namespace SmartSolutionsLab.Roomy.Identity.Application.UseCases;

// Elevates an existing account to Administrator (US4 / IA-4). Keycloak is the token authority (ADR-0013),
// so the realm role is assigned there first; only on success is the elevation recorded on the aggregate
// and committed, so a Keycloak failure leaves nothing persisted. Idempotent: an account that is already
// an administrator is a no-op — no Keycloak call, no event, no write. A missing account is an
// Error.NotFound the endpoint surfaces as 404.
public sealed class GrantAdministratorHandler(
    IUserRepository users,
    IIdentityProviderPort identityProvider,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider) : ICommandHandler<GrantAdministrator>
{
    public async Task<Result> HandleAsync(GrantAdministrator command, CancellationToken cancellationToken)
    {
        var lookup = await users.GetByIdentifierAsync(command.UserId, cancellationToken);
        if (lookup.IsFailure)
        {
            return lookup.Error;
        }

        var user = lookup.Value;
        if (user.IsAdministrator)
        {
            return Result.Success();
        }

        if (user.KeycloakSubjectIdentifier is not { } subject)
        {
            return Error.Validation(
                "user.not_active", "Only an activated account can be elevated to administrator.");
        }

        var sync = await identityProvider.AssignAdministratorRoleAsync(subject, cancellationToken);
        if (sync.IsFailure)
        {
            return sync.Error;
        }

        user.GrantAdministrator(timeProvider.GetUtcNow());
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
