namespace SmartSolutionsLab.Roomy.Attendance.Api.Endpoints.Response.Page;

// One keyset-paginated page per list (ADR-0044): the items in their stable sort order plus the opaque
// cursor that locates the next page — null when the list is exhausted. Concrete per-list records keep
// the emitted OpenAPI schema names stable for the drift gate (ADR-0036).
internal sealed record Reservation(
    IReadOnlyList<Response.Reservation> Items,
    string? NextCursor);
