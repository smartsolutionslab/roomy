namespace SmartSolutionsLab.Roomy.Attendance.Api.Endpoints.Response;

internal sealed record Reservation(
    Guid ReservationId,
    Guid OfficeId,
    Guid RoomId,
    DateOnly Date,
    Guid EmployeeId);
