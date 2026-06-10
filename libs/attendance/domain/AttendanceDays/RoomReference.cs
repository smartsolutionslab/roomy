using SmartSolutionsLab.Roomy.SharedKernel;

namespace SmartSolutionsLab.Roomy.Attendance.Domain.AttendanceDays;

public sealed record RoomReference : IValueObject
{
    public OfficeIdentifier Office { get; }

    public RoomIdentifier Room { get; }

    private RoomReference(OfficeIdentifier office, RoomIdentifier room)
    {
        Office = office;
        Room = room;
    }

    public static RoomReference From(OfficeIdentifier office, RoomIdentifier room) => new(office, room);
}
