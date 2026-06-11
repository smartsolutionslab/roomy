namespace SmartSolutionsLab.Roomy.Attendance.Api.Endpoints.Response;

internal sealed record BookableRoom(
    Guid OfficeId,
    string OfficeName,
    Guid RoomId,
    string RoomName,
    int Capacity);
