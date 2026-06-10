using SmartSolutionsLab.Roomy.Application.Contracts.Messaging;

namespace SmartSolutionsLab.Roomy.Identity.Infrastructure.Persistence;

// EF Core unit of work for use cases invoked outside the messaging pipeline (where Wolverine
// auto-applies the transaction, ADR-0005). Commits the changes the repository tracked on the context.
public sealed class IdentityUnitOfWork(IdentityDbContext context) : IUnitOfWork
{
    public Task SaveChangesAsync(CancellationToken cancellationToken) =>
        context.SaveChangesAsync(cancellationToken);
}
