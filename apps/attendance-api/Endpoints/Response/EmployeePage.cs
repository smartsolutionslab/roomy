namespace SmartSolutionsLab.Roomy.Attendance.Api.Endpoints.Response;

// One keyset-paginated page of the employee directory (ADR-0044); see ReservationPage for the
// per-list page-record rationale.
internal sealed record EmployeePage(IReadOnlyList<EmployeeResponse> Items, string? NextCursor);
