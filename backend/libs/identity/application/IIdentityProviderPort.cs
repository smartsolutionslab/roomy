using SmartSolutionsLab.Roomy.Identity.Domain.Users;
using SmartSolutionsLab.Roomy.SharedKernel.Results;

namespace SmartSolutionsLab.Roomy.Identity.Application;

public interface IIdentityProviderPort
{
    Task<Result<KeycloakSubjectIdentifier>> ProvisionUserAsync(
        UserIdentifier userIdentifier,
        Email email,
        DisplayName displayName,
        string initialPassword,
        Role role,
        CancellationToken cancellationToken);

    Task<Result> AssignAdministratorRoleAsync(KeycloakSubjectIdentifier subject, CancellationToken cancellationToken);
}
