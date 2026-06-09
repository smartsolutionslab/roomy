using SmartSolutionsLab.Roomy.Organization.Application;

namespace SmartSolutionsLab.Roomy.Organization.Infrastructure.Persistence;

public sealed class OrganizationUnitOfWork(OrganizationDbContext context) : IUnitOfWork
{
    public Task SaveChangesAsync(CancellationToken cancellationToken) =>
        context.SaveChangesAsync(cancellationToken);
}
