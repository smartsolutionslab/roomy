using System.Security.Claims;
using SmartSolutionsLab.Roomy.SharedKernel.Results;

namespace SmartSolutionsLab.Roomy.Web.Http;

public static class CurrentUser
{
    // The Keycloak subject. Identity owns the Keycloak↔Roomy mapping and resolves the account by it;
    // contexts that key on the domain UserId use UserId() instead (ADR-0058).
    extension(ClaimsPrincipal principal)
    {
        public Result<Guid> Subject()
        {
            var subject = principal.FindFirstValue(ClaimTypes.NameIdentifier) ?? principal.FindFirstValue("sub");

            return Guid.TryParse(subject, out var identifier)
                ? identifier
                : Error.Unauthorized("no_subject", "The caller has no subject claim.");
        }
    }

    // The caller's Roomy UserIdentifier, carried in the roomy_user_id claim (ADR-0058). Differs from the
    // Keycloak sub, so contexts keying on the domain user (attendance) resolve the caller via this.
    extension(ClaimsPrincipal principal)
    {
        public Result<Guid> UserId()
        {
            var userId = principal.FindFirstValue(RoomyClaims.UserId);

            return Guid.TryParse(userId, out var identifier)
                ? identifier
                : Error.Unauthorized("no_user_id", "The caller has no Roomy user id claim.");
        }

        public bool TryGetSubject(out Guid subject)
        {
            var result = principal.Subject();
            subject = result.IsSuccess ? result.Value : default;
            return result.IsSuccess;
        }

        public bool TryGetUserId(out Guid userId)
        {
            var result = principal.UserId();
            userId = result.IsSuccess ? result.Value : default;
            return result.IsSuccess;
        }

        public bool IsAdministrator() =>
            principal.IsInRole(RoomyRoles.Administrator);
    }

    // Endpoint-friendly forms of the two reads above: true with the value when the claim is present,
    // false when the caller should be treated as unauthorized. Lets a handler write
    // `if (!principal.TryGet…(out var x)) return Results.Unauthorized();` without unpacking a Result.
}
