using SmartSolutionsLab.Roomy.Infrastructure.Persistence.EventStore;

namespace SmartSolutionsLab.Roomy.Infrastructure.Persistence.Tests.EventStore;

/// <summary>
/// Behavioural tests for the hand-rolled event store against a real (SQLite) relational provider,
/// covering the issue #19 Done criteria: append + load-by-replay works, and a stale expected
/// version is rejected by the in-code version check. The unique <c>(stream_id, version)</c> index
/// is created from the EF model, but these tests do not reproduce a true concurrent write race —
/// that DB-level guard (Postgres SQLSTATE 23505) is covered by a Testcontainers test on PostgreSQL
/// (#67); SQLite cannot reproduce it (ADR-0012).
/// </summary>
public sealed class EfCoreEventStoreTests
{
    private static readonly IEventSerializer serializer = new JsonEventSerializer(
        EventTypeRegistry.Create()
            .Register<DeskBooked>("desk-booked")
            .Register<DeskReleased>("desk-released")
            .Build());

    [Fact]
    public async Task Append_then_read_replays_events_in_version_order()
    {
        using var fixture = new SqliteEventStoreFixture();
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

            Assert.Collection(
                stream,
                first =>
                {
                    Assert.Equal(1, first.Version.Value);
                    Assert.Equal(booked, first.Event);
                },
                second =>
                {
                    Assert.Equal(2, second.Version.Value);
                    Assert.Equal(released, second.Event);
                });
        }
    }

    [Fact]
    public async Task Reading_an_unknown_stream_returns_empty()
    {
        using var fixture = new SqliteEventStoreFixture();
        await using var context = fixture.CreateContext();
        var store = fixture.CreateEventStore(context, serializer);

        var stream = await store.ReadStreamAsync(StreamId.From(Guid.NewGuid()), CancellationToken.None);

        Assert.Empty(stream);
    }

    [Fact]
    public async Task Appending_no_events_is_a_no_op()
    {
        using var fixture = new SqliteEventStoreFixture();
        var streamId = StreamId.From(Guid.NewGuid());
        await using var context = fixture.CreateContext();
        var store = fixture.CreateEventStore(context, serializer);

        await store.AppendAsync(streamId, StreamVersion.None, [], EventMetadata.None, CancellationToken.None);

        Assert.Empty(await store.ReadStreamAsync(streamId, CancellationToken.None));
    }

    [Fact]
    public async Task Append_with_a_stale_expected_version_is_rejected()
    {
        using var fixture = new SqliteEventStoreFixture();
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
            var conflict = await Assert.ThrowsAsync<EventStoreConcurrencyException>(() =>
                store.AppendAsync(
                    streamId, StreamVersion.None, [booked], EventMetadata.None, CancellationToken.None));

            Assert.Equal(streamId, conflict.StreamId);
            Assert.Equal(StreamVersion.None, conflict.ExpectedVersion);
            Assert.Equal(StreamVersion.From(1), conflict.ActualVersion);
        }
    }

    [Fact]
    public async Task A_second_writer_on_the_same_expected_version_loses_and_one_append_survives()
    {
        using var fixture = new SqliteEventStoreFixture();
        var streamId = StreamId.From(Guid.NewGuid());
        var booked = new DeskBooked(Guid.NewGuid(), "ada", new DateOnly(2026, 6, 8));

        await using var first = fixture.CreateContext();
        await using var second = fixture.CreateContext();
        var firstStore = fixture.CreateEventStore(first, serializer);
        var secondStore = fixture.CreateEventStore(second, serializer);

        await firstStore.AppendAsync(
            streamId, StreamVersion.None, [booked], EventMetadata.None, CancellationToken.None);

        // Sequential here: the second writer re-reads version 1, so the in-code version check
        // rejects it. The true concurrent race (both reading 0, the DB unique index rejecting the
        // loser via SQLSTATE 23505) is covered by the Postgres integration test (#67).
        await Assert.ThrowsAsync<EventStoreConcurrencyException>(() =>
            secondStore.AppendAsync(
                streamId, StreamVersion.None, [booked], EventMetadata.None, CancellationToken.None));

        await using var verify = fixture.CreateContext();
        var verifyStore = fixture.CreateEventStore(verify, serializer);
        Assert.Single(await verifyStore.ReadStreamAsync(streamId, CancellationToken.None));
    }

    [Fact]
    public async Task Append_preserves_event_metadata()
    {
        using var fixture = new SqliteEventStoreFixture();
        var streamId = StreamId.From(Guid.NewGuid());
        var metadata = new EventMetadata(Guid.NewGuid(), Guid.NewGuid(), "ada");
        var booked = new DeskBooked(Guid.NewGuid(), "ada", new DateOnly(2026, 6, 8));

        await using var context = fixture.CreateContext();
        var store = fixture.CreateEventStore(context, serializer);
        await store.AppendAsync(streamId, StreamVersion.None, [booked], metadata, CancellationToken.None);

        var stream = await store.ReadStreamAsync(streamId, CancellationToken.None);

        Assert.Equal(metadata, Assert.Single(stream).Metadata);
    }
}
