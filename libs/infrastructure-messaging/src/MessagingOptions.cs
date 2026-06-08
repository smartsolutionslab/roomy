namespace SmartSolutionsLab.Roomy.Infrastructure.Messaging;

/// <summary>
/// Composition-root configuration for the messaging backbone, bound from the <c>Messaging</c>
/// configuration section. Keeps the transport choice (ADR-0015) and the broker/persistence
/// connection strings out of code so a deployment selects its transport without a rebuild.
/// </summary>
public sealed class MessagingOptions
{
    /// <summary>The configuration section these options bind from.</summary>
    public const string SectionName = "Messaging";

    /// <summary>The selected transport. Defaults to <see cref="MessagingTransport.RabbitMq"/> (ADR-0015).</summary>
    public MessagingTransport Transport { get; set; } = MessagingTransport.RabbitMq;

    /// <summary>
    /// The transport's connection string (e.g. the RabbitMQ AMQP URI). Resolved from configuration
    /// at the composition root — typically the Aspire-injected connection string for the broker.
    /// </summary>
    public string? ConnectionString { get; set; }

    /// <summary>
    /// The PostgreSQL connection string backing Wolverine's durable transactional outbox/inbox. Must
    /// point at the same database as the context's <c>RoomyDbContext</c> so the outbox enrolls in the
    /// aggregate-write transaction (ADR-0012:76).
    /// </summary>
    public string? PostgresConnectionString { get; set; }
}
