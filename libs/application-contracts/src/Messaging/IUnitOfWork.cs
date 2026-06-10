namespace SmartSolutionsLab.Roomy.Application.Contracts.Messaging;

// Commit seam for a use case that mutates an aggregate outside the messaging pipeline, where Wolverine
// already auto-applies the transaction (ADR-0005). The repository only tracks changes (ADR-0012); this port
// commits them. Infrastructure implements it over the context's DbContext. Shared by the contexts that use a
// state-based store (identity, organization); event-sourced contexts have no explicit commit seam.
public interface IUnitOfWork
{
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
