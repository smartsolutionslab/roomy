using SmartSolutionsLab.Roomy.Application.Contracts.Messaging;
using SmartSolutionsLab.Roomy.Attendance.Domain.AttendanceDays;

namespace SmartSolutionsLab.Roomy.Attendance.Application.UseCases;

// Intent to cancel a reservation, freeing its place (FR-008). The company-day locates the aggregate
// (ADR-0026); the date is carried because the event-sourced stream is keyed by company-day, so the
// reservation id alone cannot address it. Actor + admin flag drive the owner-or-admin check (FR-012);
// their resolution from the session is hardened in US4.
public sealed record CancelReservation(
    CompanyIdentifier Company,
    ReservationIdentifier Reservation,
    BookingDate Date,
    EmployeeIdentifier Actor,
    bool ActorIsAdmin) : ICommand;
