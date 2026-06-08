namespace SmartSolutionsLab.Roomy.Identity.Domain.Users;

// Persistence port for the User aggregate. The contract lives in the domain, next to its aggregate
// (csharp standards); EF Core implements it in infrastructure (ADR-0012). FindByEmail backs the
// email-uniqueness guard (FR-009, research R8); the unit-of-work / outbox commit is infrastructure.
public interface IUserRepository
{
    Task<User?> FindByIdentifierAsync(UserIdentifier identifier, CancellationToken cancellationToken);

    Task<User?> FindByEmailAsync(Email email, CancellationToken cancellationToken);

    Task AddAsync(User user, CancellationToken cancellationToken);
}
