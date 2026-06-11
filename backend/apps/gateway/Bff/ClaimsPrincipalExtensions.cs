using System.Security.Claims;
using Microsoft.IdentityModel.JsonWebTokens;

using SmartSolutionsLab.Roomy.Gateway.Bff.Response;
namespace SmartSolutionsLab.Roomy.Gateway.Bff;

public static class ClaimsPrincipalExtensions
{
    public static CurrentUser ToCurrentUser(this ClaimsPrincipal principal)
    {
        var name = principal.FindFirstValue(JwtRegisteredClaimNames.PreferredUsername)
            ?? principal.FindFirstValue(JwtRegisteredClaimNames.Name)
            ?? principal.Identity?.Name
            ?? string.Empty;

        var roles = principal.FindAll(ClaimTypes.Role)
            .Select(role => role.Value)
            .Where(role => !string.IsNullOrWhiteSpace(role))
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        return new CurrentUser(name, roles);
    }
}
