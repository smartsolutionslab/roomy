namespace SmartSolutionsLab.Roomy.Attendance.Api.Endpoints;

internal sealed record BookableRoomResponse(Guid OfficeId, string OfficeName, Guid RoomId, string RoomName, int Capacity);
