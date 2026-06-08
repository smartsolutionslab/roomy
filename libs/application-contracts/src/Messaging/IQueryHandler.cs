using SmartSolutionsLab.Roomy.SharedKernel.Results;

namespace SmartSolutionsLab.Roomy.Application.Contracts.Messaging;

/// <summary>
/// Handles a single <see cref="IQuery{TResult}"/>, returning a <see cref="Result{TResult}"/>
/// with the requested projection on success or an <see cref="Error"/> when it cannot be served.
/// </summary>
/// <typeparam name="TQuery">The query this handler processes.</typeparam>
/// <typeparam name="TResult">The value the query returns.</typeparam>
public interface IQueryHandler<in TQuery, TResult>
    where TQuery : IQuery<TResult>
{
    Task<Result<TResult>> HandleAsync(TQuery query, CancellationToken cancellationToken);
}
