namespace SmartSolutionsLab.Roomy.Attendance.Api.Endpoints.Response;

internal sealed record OfficeOccupancy(Guid OfficeId, string Name, int Occupied, int Capacity, bool IsFull);
