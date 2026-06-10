namespace SmartSolutionsLab.Roomy.Application.Contracts.Messaging;

public interface IUnitOfWork
{
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
