using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;

namespace SmartSolutionsLab.Roomy.Web.Http;

public static class AdministratorAuthorization
{
    public static RouteHandlerBuilder RequireAdministrator(this RouteHandlerBuilder builder) =>
        builder.RequireAuthorization(policy => policy.RequireRole(RoomyRoles.Administrator));
}
