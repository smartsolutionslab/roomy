using Shouldly;
using SmartSolutionsLab.Roomy.Infrastructure.Persistence.EventStore;

namespace SmartSolutionsLab.Roomy.Infrastructure.Persistence.Tests.EventStore;

// Exercises append/replay, the in-code expected-version check, and metadata round-trips against the real
// PostgreSQL provider (ADR-0012) — the same engine production uses, including its unique
// (stream_id, version) index. Each test uses a fresh random stream id, so they share the one database
// without interfering. The DB-level unique-violation (23505) race is covered by
// EventStoreConcurrencyRaceTests (#67).
public sealed class EfCoreEventStoreTests(PostgresEventStoreFixture fixture)
    : IClassFixture<PostgresEventStoreFixture>
{
    private static readonly IEventSerializer serializer = new JsonEventSerializer(
        EventTypeRegistry.Create()
            .Register<DeskBooked>("desk-booked")
            .Register<DeskReleased>("desk-released")
            .Build());

    [Fact]
    public async Task Append_then_read_replays_events_in_version_order()
    {
        var streamId = StreamId.From(Guid.NewGuid());
        var booked = new DeskBooked(Guid.NewGuid(), "ada", new DateOnly(2026, 6, 8));
        var released = new DeskReleased(booked.DeskId, "ada");

        await using (var context = fixture.CreateContext())
        {
            var store = fixture.CreateEventStore(context, serializer);
            await store.AppendAsync(
                streamId, StreamVersion.None, [booked, released], EventMetadata.None, CancellationToken.None);
        }

        await using (var context = fixture.CreateContext())
        {
            var store = fixture.CreateEventStore(context, serializer);
            var stream = await store.ReadStreamAsync(streamId, CancellationToken.None);

            stream.Count.ShouldBe(2);
            stream[0].Version.Value.ShouldBe(1);
            stream[0].Event.ShouldBe(booked);
            stream[1].Version.Value.ShouldBe(2);
            stream[1].Event.ShouldBe(released);
        }
    }

    [Fact]
    public async Task Reading_an_unknown_stream_returns_empty()
    {
        await using var context = fixture.CreateContext();
        var store = fixture.CreateEventStore(context, serializer);

        var stream = await store.ReadStreamAsync(StreamId.From(Guid.NewGuid()), CancellationToken.None);

        stream.ShouldBeEmpty();
    }

    [Fact]
    public async Task Appending_no_events_is_a_no_op()
    {
        var streamId = StreamId.From(Guid.NewGuid());
        await using var context = fixture.CreateContext();
        var store = fixture.CreateEventStore(context, serializer);

        await store.AppendAsync(streamId, StreamVersion.None, [], EventMetadata.None, CancellationToken.None);

        (await store.ReadStreamAsync(streamId, CancellationToken.None)).ShouldBeEmpty();
    }

    [Fact]
    public async Task Append_with_a_stale_expected_version_is_rejected()
    {
        var streamId = StreamId.From(Guid.NewGuid());
        var booked = new DeskBooked(Guid.NewGuid(), "ada", new DateOnly(2026, 6, 8));

        await using (var context = fixture.CreateContext())
        {
            var store = fixture.CreateEventStore(context, serializer);
            await store.AppendAsync(
                streamId, StreamVersion.None, [booked], EventMetadata.None, CancellationToken.None);
        }

        await using (var context = fixture.CreateContext())
        {
            var store = fixture.CreateEventStore(context, serializer);

            // The stream is now at version 1, but we still assert it is empty (None).
            var conflict = await Should.ThrowAsync<EventStoreConcurrencyException>(() =>
                store.AppendAsync(
                    streamId, StreamVersion.None, [booked], EventMetadata.None, CancellationToken.None));

            conflict.StreamId.ShouldBe(streamId);
            conflict.ExpectedVersion.ShouldBe(StreamVersion.None);
            conflict.ActualVersion.ShouldBe(StreamVersion.From(1));
        }
    }

    [Fact]
    public async Task A_second_writer_on_the_same_expected_version_loses_and_one_append_survives()
    {
        var streamId = StreamId.From(Guid.NewGuid());
        var booked = new DeskBooked(Guid.NewGuid(), "ada", new DateOnly(2026, 6, 8));

        await using var first = fixture.CreateContext();
        await using var second = fixture.CreateContext();
        var firstStore = fixture.CreateEventStore(first, serializer);
        var secondStore = fixture.CreateEventStore(second, serializer);

        await firstStore.AppendAsync(
            streamId, StreamVersion.None, [booked], EventMetadata.None, CancellationToken.None);

        // Sequential here: the second writer re-reads version 1, so the in-code version check rejects it.
        // The true concurrent race (both reading 0, the DB unique index rejecting the loser via SQLSTATE
        // 23505) is covered by EventStoreConcurrencyRaceTests (#67).
        await Should.ThrowAsync<EventStoreConcurrencyException>(() =>
            secondStore.AppendAsync(
                streamId, StreamVersion.None, [booked], EventMetadata.None, CancellationToken.None));

        await using var verify = fixture.CreateContext();
        var verifyStore = fixture.CreateEventStore(verify, serializer);
        (await verifyStore.ReadStreamAsync(streamId, CancellationToken.None)).ShouldHaveSingleItem();
    }

    [Fact]
    public async Task Append_preserves_event_metadata()
    {
        var streamId = StreamId.From(Guid.NewGuid());
        var metadata = new EventMetadata(Guid.NewGuid(), Guid.NewGuid(), "ada");
        var booked = new DeskBooked(Guid.NewGuid(), "ada", new DateOnly(2026, 6, 8));

        await using var context = fixture.CreateContext();
        var store = fixture.CreateEventStore(context, serializer);
        await store.AppendAsync(streamId, StreamVersion.None, [booked], metadata, CancellationToken.None);

        var stream = await store.ReadStreamAsync(streamId, CancellationToken.None);

        stream.ShouldHaveSingleItem().Metadata.ShouldBe(metadata);
    }
}
