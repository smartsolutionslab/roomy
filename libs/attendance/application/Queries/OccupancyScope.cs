using SmartSolutionsLab.Roomy.Attendance.Domain.AttendanceDays;

namespace SmartSolutionsLab.Roomy.Attendance.Application.Queries;

// What an occupancy query is scoped to: a whole office (its rooms and their rollup) or a single room
// (FR-001/002/005). Exactly one is set; the static factories are the only way to build it, so an invalid
// "neither/both" scope cannot be expressed — the endpoint maps a missing or ambiguous scope to a 422
// before constructing the query.
public sealed record OccupancyScope
{
    private OccupancyScope(OfficeIdentifier? office, RoomIdentifier? room)
    {
        Office = office;
        Room = room;
    }

    public OfficeIdentifier? Office { get; }

    public RoomIdentifier? Room { get; }

    public static OccupancyScope ForOffice(OfficeIdentifier office) => new(office, null);

    public static OccupancyScope ForRoom(RoomIdentifier room) => new(null, room);
}
