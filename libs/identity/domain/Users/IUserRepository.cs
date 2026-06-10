using SmartSolutionsLab.Roomy.SharedKernel.Pagination;
using SmartSolutionsLab.Roomy.SharedKernel.Results;

namespace SmartSolutionsLab.Roomy.Identity.Domain.Users;

public interface IUserRepository
{
    Task<Result<User>> GetByIdentifierAsync(UserIdentifier identifier, CancellationToken cancellationToken);

    Task<Result<User>> GetByKeycloakSubjectAsync(KeycloakSubjectIdentifier subject, CancellationToken cancellationToken);

    Task<bool> ExistsByEmailAsync(Email email, CancellationToken cancellationToken);

    Task<Result<Page<User>>> GetPageAsync(PageRequest request, CancellationToken cancellationToken);

    Task AddAsync(User user, CancellationToken cancellationToken);
}
