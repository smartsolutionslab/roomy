namespace Microsoft.Extensions.Configuration;

// A connection string that must be present. Each context owns its database and Aspire injects it (and the
// broker) by name (ADR-0014); a missing one is a fatal misconfiguration, not a recoverable condition.
// Mirrors the framework's GetConnectionString but fails fast with the resource name instead of returning
// null, so every host's composition root reads the required string the same way.
public static class ConnectionStringConfigurationExtensions
{
    public static string GetRequiredConnectionString(this IConfiguration configuration, string name) =>
        configuration.GetConnectionString(name)
            ?? throw new InvalidOperationException($"Missing connection string '{name}'.");
}
