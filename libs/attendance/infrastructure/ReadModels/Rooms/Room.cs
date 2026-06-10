namespace SmartSolutionsLab.Roomy.Attendance.Infrastructure.ReadModels.Rooms;

public sealed class Room
{
    public required Guid RoomId { get; init; }

    public required Guid OfficeId { get; init; }

    public required Guid CompanyId { get; init; }

    public required int Capacity { get; set; }

    public required string Name { get; set; }
}
