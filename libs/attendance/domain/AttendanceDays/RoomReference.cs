using SmartSolutionsLab.Roomy.SharedKernel;

namespace SmartSolutionsLab.Roomy.Attendance.Domain.AttendanceDays;

// A room together with the office it lives in — a reservation targets a room *in* an office
// (booking flow office → room → reserve, spec). Pairing the two ids keeps a reservation's location
// whole without reaching into organization's model (ADR-0014).
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
