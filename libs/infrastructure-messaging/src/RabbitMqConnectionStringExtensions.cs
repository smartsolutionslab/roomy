using Microsoft.Extensions.Configuration;

namespace SmartSolutionsLab.Roomy.Infrastructure.Messaging;

// The RabbitMQ broker connection string, injected by Aspire under the "rabbitmq" resource name. RabbitMQ
// is the default transport (ADR-0015); each host reads it the same way when wiring the messaging runtime.
public static class RabbitMqConnectionStringExtensions
{
    public static string GetRabbitMqConnectionString(this IConfiguration configuration) =>
        configuration.GetRequiredConnectionString("rabbitmq");
}
