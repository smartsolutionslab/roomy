namespace SmartSolutionsLab.Roomy.Identity.Api.Endpoints;

// The admin account projection returned by GET /admin/users and /admin/users/{id} (identity-api.md).
// Like AccountResponse but with the account status the admin overview needs; role is the flattened
// claim — "employee" or "administrator".
public sealed record AdminUserResponse(
    Guid UserId, string Email, string DisplayName, string Role, string Status);

// One keyset-paginated page of accounts (ADR-0042): the accounts in email order plus the opaque
// cursor that locates the next page — null when the list is exhausted.
public sealed record AdminUserPage(IReadOnlyList<AdminUserResponse> Items, string? NextCursor);
