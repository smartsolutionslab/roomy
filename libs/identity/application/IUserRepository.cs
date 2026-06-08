using SmartSolutionsLab.Roomy.Identity.Domain;
using SmartSolutionsLab.Roomy.Identity.Domain.ValueObjects;

namespace SmartSolutionsLab.Roomy.Identity.Application;

// Persistence port for the User aggregate (ADR-0003): the application defines it; the
// infrastructure layer implements it on EF Core (ADR-0012). FindByEmail backs the email-uniqueness
// guard (FR-009, research R8); the unit-of-work / outbox commit is an infrastructure concern.
public interface IUserRepository
{
    Task<User?> FindByIdAsync(UserId id, CancellationToken cancellationToken);

    Task<User?> FindByEmailAsync(Email email, CancellationToken cancellationToken);

    Task AddAsync(User user, CancellationToken cancellationToken);
}
