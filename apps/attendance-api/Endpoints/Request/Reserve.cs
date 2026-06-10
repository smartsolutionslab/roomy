namespace SmartSolutionsLab.Roomy.Attendance.Api.Endpoints.Request;

internal sealed record Reserve(
    Guid OfficeId,
    Guid RoomId,
    DateOnly Date,
    Guid? OnBehalfOf = null);
