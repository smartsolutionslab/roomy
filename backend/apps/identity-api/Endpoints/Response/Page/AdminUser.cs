namespace SmartSolutionsLab.Roomy.Identity.Api.Endpoints.Response.Page;

public sealed record AdminUser(IReadOnlyList<Response.AdminUser> Items, string? NextCursor);
