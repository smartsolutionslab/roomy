namespace SmartSolutionsLab.Roomy.Attendance.Infrastructure.ReadModels.Rooms;

// Attendance's local mirror of a room's capacity — master data owned by organization, fed in by the
// RoomAdded integration event (ADR-0014/0037, 003 US2). The reserve use case reads Capacity from here
// (via RoomDirectory) to enforce no-overbooking, so attendance never joins to organization's database.
// A plain read-model row, rebuildable by replaying the feed.
public sealed class Room
{
    public required Guid RoomId { get; init; }

    public required Guid OfficeId { get; init; }

    public required Guid CompanyId { get; init; }

    public required int Capacity { get; set; }

    public required string Name { get; set; }
}
