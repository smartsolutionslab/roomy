using Shouldly;
using SmartSolutionsLab.Roomy.Infrastructure.Persistence.EventStore;

namespace SmartSolutionsLab.Roomy.Infrastructure.Persistence.Tests.EventStore;

// The event store's optimistic concurrency has two layers (ADR-0012): an in-code expected-version check
// and the DB unique (stream_id, version) index. The other event-store tests exercise the in-code check
// with sequential appends. This drives a GENUINE concurrent append against real PostgreSQL — two writers,
// each on its own DbContext, appending the first event to the same new stream at the same expected
// version at the same time — so both pass the in-code check and the loser is rejected by the unique index,
// surfacing via SQLSTATE 23505 -> EventStoreConcurrencyException (issue #67). SQLite cannot emit 23505.
// The outcome (exactly one winner, one event in the stream) holds for either rejection path, so the test
// asserts the guarantee without flaking on the exact timing.
public sealed class EventStoreConcurrencyRaceTests(PostgresEventStoreFixture fixture)
    : IClassFixture<PostgresEventStoreFixture>
{
    private static readonly IEventSerializer serializer = new JsonEventSerializer(
        EventTypeRegistry.Create()
            .Register<DeskBooked>("desk-booked")
            .Build());

    [Fact]
    public async Task Two_concurrent_first_appends_let_exactly_one_win_via_the_unique_index()
    {
        var streamId = StreamId.From(Guid.NewGuid());

        var first = AppendFirstEventAsync(streamId);
        var second = AppendFirstEventAsync(streamId);
        var won = await Task.WhenAll(first, second);

        won.Count(success => success).ShouldBe(1);
        won.Count(success => !success).ShouldBe(1);

        await using var context = fixture.CreateContext();
        var stream = await fixture.CreateEventStore(context, serializer)
            .ReadStreamAsync(streamId, TestContext.Current.CancellationToken);
        stream.Count.ShouldBe(1);
    }

    private async Task<bool> AppendFirstEventAsync(StreamId streamId)
    {
        await using var context = fixture.CreateContext();
        try
        {
            await fixture.CreateEventStore(context, serializer).AppendAsync(
                streamId,
                StreamVersion.None,
                [new DeskBooked(
                    Guid.NewGuid(),
                    "ada",
                    new DateOnly(2026, 6, 8))],
                EventMetadata.None,
                TestContext.Current.CancellationToken);
            return true;
        }
        catch (EventStoreConcurrencyException)
        {
            return false;
        }
    }
}
