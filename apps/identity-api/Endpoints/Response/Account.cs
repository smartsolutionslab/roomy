namespace SmartSolutionsLab.Roomy.Identity.Api.Endpoints.Response;

public sealed record Account(Guid UserId, string Email, string DisplayName, string Role);
