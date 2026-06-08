using Microsoft.EntityFrameworkCore;
using SmartSolutionsLab.Roomy.Identity.Domain.Users;
using SmartSolutionsLab.Roomy.SharedKernel.Results;

namespace SmartSolutionsLab.Roomy.Identity.Infrastructure.Persistence;

// EF Core implementation of the User persistence port (ADR-0012). It only tracks changes; the
// unit-of-work commit (and the transactional outbox) is the host's concern. Absence is never a null:
// a missing identifier is an Error.NotFound, and the uniqueness guard is a boolean existence check.
public sealed class UserRepository(IdentityDbContext context) : IUserRepository
{
    public async Task<Result<User>> GetByIdentifierAsync(
        UserIdentifier identifier,
        CancellationToken cancellationToken)
    {
        var user = await context.Users
            .SingleOrDefaultAsync(candidate => candidate.Identifier == identifier, cancellationToken);

        if (user is null)
        {
            return Error.NotFound("user.not_found", $"No user exists with identifier '{identifier}'.");
        }

        return user;
    }

    public Task<bool> ExistsByEmailAsync(Email email, CancellationToken cancellationToken) =>
        context.Users.AnyAsync(user => user.Email == email, cancellationToken);

    public async Task AddAsync(User user, CancellationToken cancellationToken) =>
        await context.Users.AddAsync(user, cancellationToken);
}
