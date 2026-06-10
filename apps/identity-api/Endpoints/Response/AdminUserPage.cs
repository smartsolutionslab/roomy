namespace SmartSolutionsLab.Roomy.Identity.Api.Endpoints.Response;

// One keyset-paginated page of accounts (ADR-0044): the accounts in email order plus the opaque
// cursor that locates the next page — null when the list is exhausted.
public sealed record AdminUserPage(IReadOnlyList<AdminUserResponse> Items, string? NextCursor);
