using SmartSolutionsLab.Roomy.SharedKernel.Results;

namespace SmartSolutionsLab.Roomy.Application.Contracts.Messaging;

/// <summary>
/// The owned entry point for sending a command to its handler. Callers depend on this port, not
/// on any concrete dispatcher; the in-process implementation (and, later, any framework adapter)
/// is wired only at the composition root (ADR-0005).
/// </summary>
public interface ICommandDispatcher
{
    Task<Result> SendAsync(ICommand command, CancellationToken cancellationToken);

    Task<Result<TResult>> SendAsync<TResult>(ICommand<TResult> command, CancellationToken cancellationToken);
}
