namespace SmartSolutionsLab.Roomy.Identity.Api.Endpoints.Response.Page;

// One keyset-paginated page of accounts (ADR-0044): the accounts in email order plus the opaque
// cursor that locates the next page — null when the list is exhausted.
public sealed record AdminUser(IReadOnlyList<Response.AdminUser> Items, string? NextCursor);
