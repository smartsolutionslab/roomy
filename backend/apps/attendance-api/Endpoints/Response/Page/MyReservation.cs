namespace SmartSolutionsLab.Roomy.Attendance.Api.Endpoints.Response.Page;

internal sealed record MyReservation(
    IReadOnlyList<Response.MyReservation> Items,
    string? NextCursor);
