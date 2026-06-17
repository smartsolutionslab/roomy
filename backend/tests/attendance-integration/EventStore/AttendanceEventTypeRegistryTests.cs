using Shouldly;
using SmartSolutionsLab.Roomy.Attendance.Domain.AttendanceDays.Events;
using SmartSolutionsLab.Roomy.Attendance.Infrastructure.Persistence;
using SmartSolutionsLab.Roomy.Infrastructure.Persistence.EventStore;

namespace SmartSolutionsLab.Roomy.Attendance.IntegrationTests.EventStore;

public class AttendanceEventTypeRegistryTests
{
    private static JsonEventSerializer CreateSerializer() => new(AttendanceEventTypeRegistry.Build());

    [Fact]
    public void Reservation_placed_serializes_under_its_stable_name()
    {
        var serialized = CreateSerializer().Serialize(SampleReservationPlaced());

        serialized.TypeName.ShouldBe("attendance.reservation-placed.v1");
    }

    [Fact]
    public void Reservation_cancelled_serializes_under_its_stable_name()
    {
        var serialized = CreateSerializer().Serialize(SampleReservationCancelled());

        serialized.TypeName.ShouldBe("attendance.reservation-cancelled.v1");
    }

    [Fact]
    public void Reservation_placed_round_trips_through_serialize_and_deserialize()
    {
        var serializer = CreateSerializer();
        var original = SampleReservationPlaced();

        var serialized = serializer.Serialize(original);
        var restored = serializer.Deserialize(serialized.TypeName, serialized.Payload);

        restored.ShouldBe(original);
    }

    [Fact]
    public void Reservation_cancelled_round_trips_through_serialize_and_deserialize()
    {
        var serializer = CreateSerializer();
        var original = SampleReservationCancelled();

        var serialized = serializer.Serialize(original);
        var restored = serializer.Deserialize(serialized.TypeName, serialized.Payload);

        restored.ShouldBe(original);
    }

    private static ReservationPlaced SampleReservationPlaced() =>
        new(
            ReservationId: Guid.CreateVersion7(),
            CompanyId: Guid.CreateVersion7(),
            Date: new DateOnly(2026, 6, 9),
            EmployeeId: Guid.CreateVersion7(),
            OfficeId: Guid.CreateVersion7(),
            RoomId: Guid.CreateVersion7(),
            OccurredAt: new DateTimeOffset(2026, 6, 9, 8, 30, 0, TimeSpan.Zero));

    private static ReservationCancelled SampleReservationCancelled() =>
        new(
            ReservationId: Guid.CreateVersion7(),
            CompanyId: Guid.CreateVersion7(),
            Date: new DateOnly(2026, 6, 9),
            EmployeeId: Guid.CreateVersion7(),
            RoomId: Guid.CreateVersion7(),
            OccurredAt: new DateTimeOffset(2026, 6, 9, 17, 0, 0, TimeSpan.Zero));
}
