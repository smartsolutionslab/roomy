namespace SmartSolutionsLab.Roomy.Infrastructure.Messaging;

/// <summary>
/// The selectable message transport (ADR-0015). The transport sits behind Wolverine's
/// multi-transport support and the owned messaging port, so switching is a composition-root + config
/// change and never touches <c>domain</c>/<c>application</c>. RabbitMQ is the default;
/// <see cref="AzureServiceBus"/> and <see cref="AmazonSqs"/> are the planned alternatives whose
/// wiring is added when an environment needs them — the selection seam exists now so they slot in
/// without changing the publish path.
/// </summary>
public enum MessagingTransport
{
    /// <summary>RabbitMQ — the default, self-hostable broker (the only transport wired today).</summary>
    RabbitMq = 0,

    /// <summary>Azure Service Bus (planned; not yet wired).</summary>
    AzureServiceBus = 1,

    /// <summary>AWS SQS + SNS (planned; not yet wired).</summary>
    AmazonSqs = 2,
}
