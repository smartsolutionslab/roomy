namespace SmartSolutionsLab.Roomy.Application.Contracts.Messaging;

/// <summary>
/// Marks a use-case input that reads state without mutating it and returns a projection.
/// Handled by exactly one <see cref="IQueryHandler{TQuery, TResult}"/>.
/// </summary>
/// <typeparam name="TResult">The value the query returns.</typeparam>
/// <remarks>
/// Part of the owned dispatch abstractions that keep the application layer framework-free
/// (ADR-0005, constitution Principle IV).
/// </remarks>
public interface IQuery<TResult>;
