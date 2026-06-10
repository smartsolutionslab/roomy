namespace SmartSolutionsLab.Roomy.Attendance.Api.Endpoints.Response;

internal sealed record ReservationResponse(Guid ReservationId, Guid OfficeId, Guid RoomId, DateOnly Date, Guid EmployeeId);
