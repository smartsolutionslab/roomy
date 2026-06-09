using Shouldly;
using SmartSolutionsLab.Roomy.Attendance.Domain.AttendanceDays;
using SmartSolutionsLab.Roomy.Attendance.Infrastructure.Messaging;
using SmartSolutionsLab.Roomy.Attendance.Infrastructure.ReadModels.Rooms;
using SmartSolutionsLab.Roomy.Contracts.Organization;

namespace SmartSolutionsLab.Roomy.Attendance.IntegrationTests;

// The capacity feed against real PostgreSQL (003 US2): organization's RoomAdded is mirrored onto the
// local Rooms read model by the consumer, and RoomDirectory then answers the reserve use case's
// capacity lookup from it (ADR-0014/0037). An unknown room is unknown_room; a repeated RoomAdded
// updates in place (idempotent, so at-least-once delivery is safe).
public sealed class RoomCapacityFeedTests(PostgresEventStoreFixture fixture) : IClassFixture<PostgresEventStoreFixture>
{
    private static readonly DateTimeOffset occurredAt = new(2026, 6, 9, 9, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task A_room_added_event_is_mirrored_and_queryable_by_capacity()
    {
        var roomId = Guid.CreateVersion7();
        await ConsumeAsync(new RoomAdded(roomId, Guid.CreateVersion7(), Guid.CreateVersion7(), "A1", 8, occurredAt));

        await using var query = fixture.CreateDbContext();
        var capacity = await new RoomDirectory(query)
            .FindCapacityAsync(RoomIdentifier.From(roomId), TestContext.Current.CancellationToken);

        capacity.IsSuccess.ShouldBeTrue();
        capacity.Value.Value.ShouldBe(8);
    }

    [Fact]
    public async Task An_unknown_room_is_not_found()
    {
        await using var query = fixture.CreateDbContext();

        var result = await new RoomDirectory(query)
            .FindCapacityAsync(RoomIdentifier.New(), TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("unknown_room");
    }

    [Fact]
    public async Task A_repeated_room_added_updates_the_capacity_in_place()
    {
        var roomId = Guid.CreateVersion7();
        var office = Guid.CreateVersion7();
        var company = Guid.CreateVersion7();
        await ConsumeAsync(new RoomAdded(roomId, office, company, "A1", 8, occurredAt));
        await ConsumeAsync(new RoomAdded(roomId, office, company, "A1", 5, occurredAt));

        await using var query = fixture.CreateDbContext();
        var capacity = await new RoomDirectory(query)
            .FindCapacityAsync(RoomIdentifier.From(roomId), TestContext.Current.CancellationToken);

        capacity.Value.Value.ShouldBe(5);
    }

    private async Task ConsumeAsync(RoomAdded message)
    {
        await using var context = fixture.CreateDbContext();
        await new RoomAddedConsumer(context).Handle(message, TestContext.Current.CancellationToken);
    }
}
