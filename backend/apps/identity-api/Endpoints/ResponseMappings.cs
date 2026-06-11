using SmartSolutionsLab.Roomy.Identity.Domain.Users;
using SmartSolutionsLab.Roomy.SharedKernel.Pagination;
namespace SmartSolutionsLab.Roomy.Identity.Api.Endpoints;

internal static class ResponseMappings
{
    extension(User user)
    {
        public Response.Account ToAccount() =>
            new(user.Identifier.Value, user.Email.Value, user.DisplayName.Value, user.Role());
    }

    extension(User user)
    {
        public Response.AdminUser ToAdminUser() =>
            new(
                user.Identifier.Value,
                user.Email.Value,
                user.DisplayName.Value,
                user.Role(),
                user.Status == UserStatus.Active ? "active" : "provisioning");
    }

    extension(Page<User> page)
    {
        public Response.Page.AdminUser ToResponse() =>
            new(page.Items.Select(user => user.ToAdminUser()).ToList(), page.NextCursor);
    }

    extension(User user)
    {
        private string Role() =>
            user.IsAdministrator ? "administrator" : "employee";
    }
}
