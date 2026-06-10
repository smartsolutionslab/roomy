namespace SmartSolutionsLab.Roomy.Identity.Api.Endpoints.Response;

// The account/role projection returned by GET /account/me (contract: identity-api.md). Role is the
// flattened claim the app authorizes on — "employee" or "administrator".
public sealed record AccountResponse(Guid UserId, string Email, string DisplayName, string Role);
