using SmartSolutionsLab.Roomy.Application.Contracts.Messaging;

namespace SmartSolutionsLab.Roomy.Identity.Infrastructure.Persistence;

public sealed class IdentityUnitOfWork(IdentityDbContext context) : IUnitOfWork
{
    public Task SaveChangesAsync(CancellationToken cancellationToken) =>
        context.SaveChangesAsync(cancellationToken);
}
