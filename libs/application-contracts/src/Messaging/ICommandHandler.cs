using SmartSolutionsLab.Roomy.SharedKernel.Results;

namespace SmartSolutionsLab.Roomy.Application.Contracts.Messaging;

/// <summary>
/// Handles a single <see cref="ICommand"/>, returning a <see cref="Result"/> that distinguishes
/// success from an expected business failure (never an exception for expected outcomes).
/// </summary>
/// <typeparam name="TCommand">The command this handler processes.</typeparam>
public interface ICommandHandler<in TCommand>
    where TCommand : ICommand
{
    Task<Result> HandleAsync(TCommand command, CancellationToken cancellationToken);
}

/// <summary>
/// Handles a single <see cref="ICommand{TResult}"/>, returning a <see cref="Result{TResult}"/>
/// carrying the produced value on success or an <see cref="Error"/> on an expected failure.
/// </summary>
/// <typeparam name="TCommand">The command this handler processes.</typeparam>
/// <typeparam name="TResult">The value produced when the command succeeds.</typeparam>
public interface ICommandHandler<in TCommand, TResult>
    where TCommand : ICommand<TResult>
{
    Task<Result<TResult>> HandleAsync(TCommand command, CancellationToken cancellationToken);
}
