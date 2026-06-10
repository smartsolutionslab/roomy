namespace SmartSolutionsLab.Roomy.Attendance.Api.Endpoints;

internal sealed record OfficeOccupancyResponse(Guid OfficeId, string Name, int Occupied, int Capacity, bool IsFull);
