namespace SmartSolutionsLab.Roomy.Attendance.Api.Endpoints;

internal sealed record ReservationResponse(Guid ReservationId, Guid OfficeId, Guid RoomId, DateOnly Date, Guid EmployeeId);
