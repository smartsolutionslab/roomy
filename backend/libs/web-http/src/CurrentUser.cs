using System.Security.Claims;
using SmartSolutionsLab.Roomy.SharedKernel.Results;

namespace SmartSolutionsLab.Roomy.Web.Http;

public static class CurrentUser
{
    public static Result<Guid> Subject(this ClaimsPrincipal principal)
    {
        var subject = principal.FindFirstValue(ClaimTypes.NameIdentifier) ?? principal.FindFirstValue("sub");

        return Guid.TryParse(subject, out var identifier)
            ? identifier
            : Error.Unauthorized("no_subject", "The caller has no subject claim.");
    }

    public static bool IsAdministrator(this ClaimsPrincipal principal) =>
        principal.IsInRole(RoomyRoles.Administrator);
}
