using Microsoft.EntityFrameworkCore;
using SmartSolutionsLab.Roomy.Identity.Domain.Users;
using SmartSolutionsLab.Roomy.SharedKernel.Pagination;
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

    public async Task<Result<User>> GetByKeycloakSubjectAsync(
        KeycloakSubjectIdentifier subject,
        CancellationToken cancellationToken)
    {
        var user = await context.Users
            .SingleOrDefaultAsync(candidate => candidate.KeycloakSubjectIdentifier == subject, cancellationToken);

        if (user is null)
        {
            return Error.NotFound("user.not_found", $"No user is linked to Keycloak subject '{subject}'.");
        }

        return user;
    }

    public Task<bool> ExistsByEmailAsync(Email email, CancellationToken cancellationToken) =>
        context.Users.AnyAsync(user => user.Email == email, cancellationToken);

    // Keyset pagination over the accounts, ordered by email (ADR-0044). Email is the unique account
    // key, so a single text column is a stable total order. The keyset predicate is parameterized SQL
    // (FromSql): Npgsql does not translate string.Compare, but PostgreSQL compares `text` natively with
    // `>`, so the page is one indexed scan. Fetching limit + 1 rows reveals whether a further page
    // exists; EF materializes the User aggregate through its value converters as usual.
    public async Task<Result<Page<User>>> GetPageAsync(
        PageRequest request,
        CancellationToken cancellationToken)
    {
        var decoded = request.DecodeCursor<UserCursor>();
        if (decoded.IsFailure)
        {
            return decoded.Error;
        }

        var probeLimit = request.Limit + 1;
        var rows = decoded.Value is { } after
            ? await context.Users
                .FromSql($@"SELECT * FROM ""users"" WHERE ""email"" > {after.Email} ORDER BY ""email"" LIMIT {probeLimit}")
                .AsNoTracking()
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false)
            : await context.Users
                .FromSql($@"SELECT * FROM ""users"" ORDER BY ""email"" LIMIT {probeLimit}")
                .AsNoTracking()
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

        return Page<User>.FromProbe(
            rows,
            request.Limit,
            row => row,
            row => new UserCursor(row.Email.Value));
    }

    public async Task AddAsync(User user, CancellationToken cancellationToken) =>
        await context.Users.AddAsync(user, cancellationToken);
}
