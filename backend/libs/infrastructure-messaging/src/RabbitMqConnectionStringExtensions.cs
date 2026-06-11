using Microsoft.Extensions.Configuration;

namespace SmartSolutionsLab.Roomy.Infrastructure.Messaging;

public static class RabbitMqConnectionStringExtensions
{
    public static string GetRabbitMqConnectionString(this IConfiguration configuration) =>
        configuration.GetRequiredConnectionString("rabbitmq");
}
