using System.Security.Claims;
using SmartSolutionsLab.Roomy.SharedKernel.Results;

namespace SmartSolutionsLab.Roomy.Web.Http;

public static class CurrentUser
{
    // The Keycloak subject. Identity owns the Keycloak↔Roomy mapping and resolves the account by it;
    // contexts that key on the domain UserId use UserId() instead (ADR-0058).
    public static Result<Guid> Subject(this ClaimsPrincipal principal)
    {
        var subject = principal.FindFirstValue(ClaimTypes.NameIdentifier) ?? principal.FindFirstValue("sub");

        return Guid.TryParse(subject, out var identifier)
            ? identifier
            : Error.Unauthorized("no_subject", "The caller has no subject claim.");
    }

    // The caller's Roomy UserIdentifier, carried in the roomy_user_id claim (ADR-0058). Differs from the
    // Keycloak sub, so contexts keying on the domain user (attendance) resolve the caller via this.
    public static Result<Guid> UserId(this ClaimsPrincipal principal)
    {
        var userId = principal.FindFirstValue(RoomyClaims.UserId);

        return Guid.TryParse(userId, out var identifier)
            ? identifier
            : Error.Unauthorized("no_user_id", "The caller has no Roomy user id claim.");
    }

    public static bool IsAdministrator(this ClaimsPrincipal principal) =>
        principal.IsInRole(RoomyRoles.Administrator);
}
