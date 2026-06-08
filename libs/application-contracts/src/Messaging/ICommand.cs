namespace SmartSolutionsLab.Roomy.Application.Contracts.Messaging;

/// <summary>
/// Marks a use-case input that mutates state. A command expresses an intention to change the
/// system and is handled by exactly one <see cref="ICommandHandler{TCommand}"/>.
/// </summary>
/// <remarks>
/// This is one of the dispatch abstractions the application layer owns so the core stays free
/// of any mediator or messaging framework (ADR-0005, constitution Principle IV).
/// </remarks>
public interface ICommand;

/// <summary>
/// Marks a state-mutating use-case input that yields a value on success. Handled by exactly one
/// <see cref="ICommandHandler{TCommand, TResult}"/>.
/// </summary>
/// <typeparam name="TResult">The value produced when the command succeeds.</typeparam>
public interface ICommand<TResult>;
