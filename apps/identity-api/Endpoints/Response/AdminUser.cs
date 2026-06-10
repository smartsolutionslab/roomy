namespace SmartSolutionsLab.Roomy.Identity.Api.Endpoints.Response;

public sealed record AdminUser(
    Guid UserId, string Email, string DisplayName, string Role, string Status);
