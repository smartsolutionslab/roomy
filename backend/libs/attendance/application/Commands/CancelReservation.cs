using SmartSolutionsLab.Roomy.Application.Contracts.Messaging;
using SmartSolutionsLab.Roomy.Attendance.Domain.AttendanceDays;

namespace SmartSolutionsLab.Roomy.Attendance.Application.Commands;

public sealed record CancelReservation(
    CompanyIdentifier Company,
    ReservationIdentifier Reservation,
    BookingDate Date,
    EmployeeIdentifier Actor,
    bool ActorIsAdmin) : ICommand;
