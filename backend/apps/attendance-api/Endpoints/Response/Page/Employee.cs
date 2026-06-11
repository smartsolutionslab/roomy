namespace SmartSolutionsLab.Roomy.Attendance.Api.Endpoints.Response.Page;

internal sealed record Employee(
    IReadOnlyList<Response.Employee> Items,
    string? NextCursor);
