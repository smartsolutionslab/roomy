namespace SmartSolutionsLab.Roomy.Attendance.Infrastructure.ReadModels.Reservations;

public sealed class Reservation
{
    public required Guid ReservationId { get; init; }

    public required Guid CompanyId { get; init; }

    public required Guid EmployeeId { get; init; }

    public required Guid OfficeId { get; init; }

    public required Guid RoomId { get; init; }

    public required DateOnly Date { get; init; }
}
