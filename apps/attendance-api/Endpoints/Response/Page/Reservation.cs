namespace SmartSolutionsLab.Roomy.Attendance.Api.Endpoints.Response.Page;

internal sealed record Reservation(
    IReadOnlyList<Response.Reservation> Items,
    string? NextCursor);
