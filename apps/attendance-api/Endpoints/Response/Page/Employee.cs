namespace SmartSolutionsLab.Roomy.Attendance.Api.Endpoints.Response.Page;

// One keyset-paginated page of the employee directory (ADR-0044); see Reservation for the
// per-list page-record rationale.
internal sealed record Employee(IReadOnlyList<Response.Employee> Items, string? NextCursor);
