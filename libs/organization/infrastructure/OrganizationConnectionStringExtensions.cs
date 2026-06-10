using Microsoft.Extensions.Configuration;

namespace SmartSolutionsLab.Roomy.Organization.Infrastructure;

// The organization context's database connection string, injected by Aspire under the "organization"
// resource name (ADR-0014). Co-located with the organization persistence registration so the resource
// name lives in one place.
public static class OrganizationConnectionStringExtensions
{
    public static string GetOrganizationConnectionString(this IConfiguration configuration) =>
        configuration.GetRequiredConnectionString("organization");
}
