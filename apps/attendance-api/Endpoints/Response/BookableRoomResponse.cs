namespace SmartSolutionsLab.Roomy.Attendance.Api.Endpoints.Response;

internal sealed record BookableRoomResponse(Guid OfficeId, string OfficeName, Guid RoomId, string RoomName, int Capacity);
