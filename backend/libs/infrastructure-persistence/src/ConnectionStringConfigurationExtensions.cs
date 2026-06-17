namespace Microsoft.Extensions.Configuration;

public static class ConnectionStringConfigurationExtensions
{
    public static string GetRequiredConnectionString(this IConfiguration configuration, string name) =>
        configuration.GetConnectionString(name) ?? throw new InvalidOperationException($"Missing connection string '{name}'.");
}
