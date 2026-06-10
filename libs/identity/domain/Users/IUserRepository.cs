using SmartSolutionsLab.Roomy.SharedKernel.Pagination;
using SmartSolutionsLab.Roomy.SharedKernel.Results;

namespace SmartSolutionsLab.Roomy.Identity.Domain.Users;

// Persistence port for the User aggregate. The contract lives in the domain, next to its aggregate
// (csharp standards); EF Core implements it in infrastructure (ADR-0012). The contract never returns
// a nullable User: a fetch that may miss is a Result<User> (Error.NotFound when absent), and the
// email-uniqueness guard (FR-009, research R8) is a boolean presence check. The unit-of-work / outbox
// commit is infrastructure.
public interface IUserRepository
{
    Task<Result<User>> GetByIdentifierAsync(UserIdentifier identifier, CancellationToken cancellationToken);

    Task<Result<User>> GetByKeycloakSubjectAsync(
        KeycloakSubjectIdentifier subject,
        CancellationToken cancellationToken);

    Task<bool> ExistsByEmailAsync(Email email, CancellationToken cancellationToken);

    // One keyset-paginated page of accounts ordered by email (ADR-0042) — email is the unique account
    // key, so the cursor is a stable total order. A malformed cursor is a validation failure.
    Task<Result<Page<User>>> GetPageAsync(PageRequest request, CancellationToken cancellationToken);

    Task AddAsync(User user, CancellationToken cancellationToken);
}
