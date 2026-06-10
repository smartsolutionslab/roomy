namespace SmartSolutionsLab.Roomy.Identity.Api.Endpoints.Response;

// The admin account projection returned by GET /admin/users and /admin/users/{id} (identity-api.md).
// Like Account but with the account status the admin overview needs; role is the flattened
// claim — "employee" or "administrator".
public sealed record AdminUser(
    Guid UserId, string Email, string DisplayName, string Role, string Status);
