using SmartSolutionsLab.Roomy.SharedKernel.Results;

namespace SmartSolutionsLab.Roomy.Application.Contracts.Messaging;

/// <summary>
/// The owned entry point for sending a query to its handler. Callers depend on this port, not on
/// any concrete dispatcher; the implementation is wired only at the composition root (ADR-0005).
/// </summary>
public interface IQueryDispatcher
{
    Task<Result<TResult>> SendAsync<TResult>(IQuery<TResult> query, CancellationToken cancellationToken);
}
