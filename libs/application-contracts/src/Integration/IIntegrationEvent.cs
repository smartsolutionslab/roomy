namespace SmartSolutionsLab.Roomy.Application.Contracts.Integration;

/// <summary>
/// Marks a message published across bounded-context boundaries. Cross-context communication is by
/// ID and integration events only — never by referencing another context's aggregate types
/// (constitution Principle III).
/// </summary>
/// <remarks>
/// The transport (Wolverine over RabbitMQ by default) is an infrastructure concern; the
/// application layer depends only on this owned contract (ADR-0005).
/// </remarks>
public interface IIntegrationEvent;
