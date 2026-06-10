namespace SmartSolutionsLab.Roomy.Attendance.Api.Endpoints.Response;

internal sealed record MyReservationResponse(
    Guid ReservationId,
    Guid OfficeId,
    string OfficeName,
    Guid RoomId,
    string RoomName,
    DateOnly Date);
