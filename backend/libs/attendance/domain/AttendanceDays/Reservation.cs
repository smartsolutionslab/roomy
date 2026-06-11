using SmartSolutionsLab.Roomy.SharedKernel;

namespace SmartSolutionsLab.Roomy.Attendance.Domain.AttendanceDays;

public sealed class Reservation : IEntity
{
    internal Reservation(
        ReservationIdentifier identifier,
        EmployeeIdentifier employee,
        OfficeIdentifier office,
        RoomIdentifier room)
    {
        Identifier = identifier;
        Employee = employee;
        Office = office;
        Room = room;
    }

    public ReservationIdentifier Identifier { get; }

    public EmployeeIdentifier Employee { get; }

    public OfficeIdentifier Office { get; }

    public RoomIdentifier Room { get; }
}
