using System.Security.Claims;
using Microsoft.IdentityModel.JsonWebTokens;

namespace SmartSolutionsLab.Roomy.Gateway.Bff;

// Projects an authenticated ClaimsPrincipal into the token-free CurrentUser the SPA consumes.
// Pure mapping, unit-tested without an HTTP pipeline.
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
