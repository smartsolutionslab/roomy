namespace SmartSolutionsLab.Roomy.Identity.Application;

// Commit seam for a use case that mutates an aggregate outside the messaging pipeline, where Wolverine
// already auto-applies the transaction (ADR-0005). The repository only tracks changes (ADR-0012); this
// port commits them. Infrastructure implements it over the context's DbContext.
public interface IUnitOfWork
{
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
