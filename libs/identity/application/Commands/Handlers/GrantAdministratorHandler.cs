using SmartSolutionsLab.Roomy.Application.Contracts.Messaging;
using SmartSolutionsLab.Roomy.Identity.Domain.Users;
using SmartSolutionsLab.Roomy.SharedKernel.Results;

namespace SmartSolutionsLab.Roomy.Identity.Application.Commands.Handlers;

public sealed class GrantAdministratorHandler(IUserRepository users, IIdentityProviderPort identityProvider, IUnitOfWork unitOfWork, TimeProvider timeProvider) : ICommandHandler<GrantAdministrator>
{
    public async Task<Result> HandleAsync(GrantAdministrator command, CancellationToken cancellationToken)
    {
        var lookup = await users.GetByIdentifierAsync(command.UserId, cancellationToken);
        if (lookup.IsFailure) return lookup.Error;

        var user = lookup.Value;
        if (user.IsAdministrator) return Result.Success();

        if (user.KeycloakSubjectIdentifier is not { } subject)
        {
            return Error.Validation("user.not_active", "Only an activated account can be elevated to administrator.");
        }

        var sync = await identityProvider.AssignAdministratorRoleAsync(subject, cancellationToken);
        if (sync.IsFailure) return sync.Error;

        user.GrantAdministrator(timeProvider.GetUtcNow());
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
