namespace SmartSolutionsLab.Roomy.Infrastructure.Messaging;

public sealed class MessagingOptions
{
    public const string SectionName = "Messaging";

    public MessagingTransport Transport { get; set; } = MessagingTransport.RabbitMq;

    public string? ConnectionString { get; set; }

    public string? PostgresConnectionString { get; set; }
}
