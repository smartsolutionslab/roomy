namespace SmartSolutionsLab.Roomy.Attendance.Api.Endpoints.Response.Page;

// One keyset-paginated page of an employee's reservations (ADR-0044); see Reservation for the
// per-list page-record rationale.
internal sealed record MyReservation(
    IReadOnlyList<Response.MyReservation> Items,
    string? NextCursor);
