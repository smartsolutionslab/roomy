using SmartSolutionsLab.Roomy.SharedKernel;

namespace SmartSolutionsLab.Roomy.Attendance.Domain.AttendanceDays;

public sealed class Reservation : IEntity
{
    internal Reservation(
        ReservationIdentifier id,
        EmployeeIdentifier employee,
        OfficeIdentifier office,
        RoomIdentifier room)
    {
        Id = id;
        Employee = employee;
        Office = office;
        Room = room;
    }

    public ReservationIdentifier Id { get; }

    public EmployeeIdentifier Employee { get; }

    public OfficeIdentifier Office { get; }

    public RoomIdentifier Room { get; }
}
