using Microsoft.Extensions.Configuration;

namespace SmartSolutionsLab.Roomy.Organization.Infrastructure;

public static class OrganizationConnectionStringExtensions
{
    public static string GetOrganizationConnectionString(this IConfiguration configuration) =>
        configuration.GetRequiredConnectionString("organization");
}
