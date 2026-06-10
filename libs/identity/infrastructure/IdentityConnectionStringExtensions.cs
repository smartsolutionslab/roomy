using Microsoft.Extensions.Configuration;

namespace SmartSolutionsLab.Roomy.Identity.Infrastructure;

// The identity context's database connection string, injected by Aspire under the "identity" resource
// name (ADR-0014). Co-located with the identity persistence registration so the resource name lives in
// one place.
public static class IdentityConnectionStringExtensions
{
    public static string GetIdentityConnectionString(this IConfiguration configuration) =>
        configuration.GetRequiredConnectionString("identity");
}
