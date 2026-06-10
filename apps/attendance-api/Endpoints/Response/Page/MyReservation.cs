namespace SmartSolutionsLab.Roomy.Attendance.Api.Endpoints.Response;

// One keyset-paginated page of an employee's reservations (ADR-0044); see ReservationPage for the
// per-list page-record rationale.
internal sealed record MyReservationPage(IReadOnlyList<MyReservationResponse> Items, string? NextCursor);
