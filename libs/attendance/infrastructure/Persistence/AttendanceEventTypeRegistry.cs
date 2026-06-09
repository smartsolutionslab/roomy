using SmartSolutionsLab.Roomy.Attendance.Domain.AttendanceDays;
using SmartSolutionsLab.Roomy.Infrastructure.Persistence.EventStore;

namespace SmartSolutionsLab.Roomy.Attendance.Infrastructure.Persistence;

// Maps the attendance stream events to their stable persisted names, registered once at composition
// (ADR-0012). Names are explicit and carry an explicit .v1 so a later CLR rename or namespace move
// does not invalidate the existing log, and the first event-schema change has a cheap version to bump.
public static class AttendanceEventTypeRegistry
{
    public static IEventTypeRegistry Build() =>
        EventTypeRegistry.Create()
            .Register<ReservationPlaced>("attendance.reservation-placed.v1")
            .Register<ReservationCancelled>("attendance.reservation-cancelled.v1")
            .Build();
}
