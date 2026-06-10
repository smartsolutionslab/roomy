using SmartSolutionsLab.Roomy.Attendance.Domain.AttendanceDays;

namespace SmartSolutionsLab.Roomy.Attendance.Application.Queries;

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
