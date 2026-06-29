using Microsoft.EntityFrameworkCore;
using SmartSolutionsLab.Roomy.Identity.Domain.Users;
using SmartSolutionsLab.Roomy.SharedKernel.Pagination;
using SmartSolutionsLab.Roomy.SharedKernel.Results;

namespace SmartSolutionsLab.Roomy.Identity.Infrastructure.Persistence;

public sealed class UserRepository(IdentityDbContext context) : IUserRepository
{
    public async Task<Result<User>> GetByIdentifierAsync(UserIdentifier identifier, CancellationToken cancellationToken)
    {
        var user = await context.Users.SingleOrDefaultAsync(candidate => candidate.Identifier == identifier, cancellationToken);

        if (user is null) return Error.NotFound("user.not_found", $"No user exists with identifier '{identifier}'.");

        return user;
    }

    public async Task<Result<User>> GetByKeycloakSubjectAsync(KeycloakSubjectIdentifier subject, CancellationToken cancellationToken)
    {
        var user = await context.Users.SingleOrDefaultAsync(candidate => candidate.KeycloakSubjectIdentifier == subject, cancellationToken);

        if (user is null) return Error.NotFound("user.not_found", $"No user is linked to Keycloak subject '{subject}'.");

        return user;
    }

    public Task<bool> ExistsByEmailAsync(Email email, CancellationToken cancellationToken) =>
        context.Users.AnyAsync(user => user.Email == email, cancellationToken);

    public async Task<Page<User>> GetPageAsync(PageRequest request, CancellationToken cancellationToken)
    {
        var after = request.DecodeCursor<UserCursor>();

        var probeLimit = request.Limit + 1;
        var rows = after is { } cursor
            ? await context.Users
                .FromSql($@"SELECT * FROM ""users"" WHERE ""email"" > {cursor.Email} ORDER BY ""email"" LIMIT {probeLimit}")
                .AsNoTracking()
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false)
            : await context.Users
                .FromSql($@"SELECT * FROM ""users"" ORDER BY ""email"" LIMIT {probeLimit}")
                .AsNoTracking()
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

        return Page<User>.FromProbe(rows, request.Limit, row => row, row => new UserCursor(row.Email.Value));
    }

    public Task AddAsync(User user, CancellationToken cancellationToken)
    {
        context.Users.Add(user);
        return Task.CompletedTask;
    }
}
