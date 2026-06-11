using Microsoft.Extensions.Configuration;

namespace SmartSolutionsLab.Roomy.Identity.Infrastructure;

public static class IdentityConnectionStringExtensions
{
    public static string GetIdentityConnectionString(this IConfiguration configuration) =>
        configuration.GetRequiredConnectionString("identity");
}
