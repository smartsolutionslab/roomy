using SmartSolutionsLab.Roomy.SharedKernel.Results;

namespace SmartSolutionsLab.Roomy.Application.Contracts.Messaging;

public interface IQueryDispatcher
{
    Task<Result<TResult>> SendAsync<TResult>(IQuery<TResult> query, CancellationToken cancellationToken);
}
