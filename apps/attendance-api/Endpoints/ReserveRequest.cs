namespace SmartSolutionsLab.Roomy.Attendance.Api.Endpoints;

internal sealed record ReserveRequest(Guid OfficeId, Guid RoomId, DateOnly Date, Guid? OnBehalfOf = null);
