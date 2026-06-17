using SmartSolutionsLab.Roomy.Attendance.Domain.AttendanceDays.Events;
using SmartSolutionsLab.Roomy.Infrastructure.Persistence.EventStore;

namespace SmartSolutionsLab.Roomy.Attendance.Infrastructure.Persistence;

public static class AttendanceEventTypeRegistry
{
    public static IEventTypeRegistry Build() =>
        EventTypeRegistry.Create()
            .Register<ReservationPlaced>("attendance.reservation-placed.v1")
            .Register<ReservationCancelled>("attendance.reservation-cancelled.v1")
            .Build();
}
