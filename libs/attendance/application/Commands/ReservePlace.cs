using SmartSolutionsLab.Roomy.Application.Contracts.Messaging;
using SmartSolutionsLab.Roomy.Attendance.Domain.AttendanceDays;

namespace SmartSolutionsLab.Roomy.Attendance.Application.Commands;

// Intent to reserve a place for an employee in a specific room of an office on a day (FR-001). The
// company-day locates the aggregate (ADR-0026); capacity and "today" are resolved by the handler, not
// carried here. Yields the new reservation's identifier on success.
public sealed record ReservePlace(
    CompanyIdentifier Company,
    EmployeeIdentifier Employee,
    OfficeIdentifier Office,
    RoomIdentifier Room,
    BookingDate Date) : ICommand<ReservationIdentifier>;
