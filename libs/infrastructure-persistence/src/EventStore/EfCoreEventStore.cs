using System.Text.Json;
using Microsoft.EntityFrameworkCore;

namespace SmartSolutionsLab.Roomy.Infrastructure.Persistence.EventStore;

/// <summary>
/// EF Core / Npgsql implementation of <see cref="IEventStore"/> over the append-only events table
/// (ADR-0012). Appends are version-checked in code <em>and</em> guaranteed by the unique
/// <c>(stream_id, version)</c> constraint, so two concurrent writers asserting the same expected
/// version cannot both commit — the loser surfaces as <see cref="EventStoreConcurrencyException"/>.
/// </summary>
/// <remarks>
/// This is the v1 skeleton: it appends and replays. It does <em>not</em> own a transaction — the
/// caller commits events, Wolverine's durable outbox records, and inline projections together via
/// the context's <c>SaveChanges</c>, keeping them atomic in one Postgres transaction (ADR-0012,
/// ADR-0005). Snapshots and async catch-up projections are deferred.
/// </remarks>
public sealed class EfCoreEventStore : IEventStore
{
    private static readonly JsonSerializerOptions metadataOptions = new(JsonSerializerDefaults.Web);

    private readonly EventStoreDbContext dbContext;
    private readonly IEventSerializer serializer;
    private readonly TimeProvider timeProvider;

    public EfCoreEventStore(
        EventStoreDbContext dbContext,
        IEventSerializer serializer,
        TimeProvider timeProvider)
    {
        this.dbContext = dbContext;
        this.serializer = serializer;
        this.timeProvider = timeProvider;
    }

    public async Task AppendAsync(
        StreamId streamId,
        StreamVersion expectedVersion,
        IReadOnlyList<object> events,
        EventMetadata metadata,
        CancellationToken cancellationToken)
    {
        if (events.Count == 0)
        {
            return;
        }

        var currentVersion = await CurrentVersionAsync(streamId, cancellationToken).ConfigureAwait(false);

        if (currentVersion != expectedVersion)
        {
            throw new EventStoreConcurrencyException(streamId, expectedVersion, currentVersion);
        }

        var occurredOnUtc = timeProvider.GetUtcNow();
        var metadataJson = JsonSerializer.Serialize(metadata, metadataOptions);
        var nextVersion = expectedVersion;

        foreach (var @event in events)
        {
            var serialized = serializer.Serialize(@event);
            nextVersion = nextVersion.Next();

            dbContext.Events.Add(new StoredEvent
            {
                StreamId = streamId.Value,
                Version = nextVersion.Value,
                EventType = serialized.TypeName,
                Payload = serialized.Payload,
                Metadata = metadataJson,
                OccurredOnUtc = occurredOnUtc,
            });
        }

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (DbUpdateException exception) when (IsUniqueViolation(exception))
        {
            var actualVersion = await CurrentVersionAsync(streamId, cancellationToken).ConfigureAwait(false);
            throw new EventStoreConcurrencyException(streamId, expectedVersion, actualVersion);
        }
    }

    public async Task<IReadOnlyList<EventEnvelope>> ReadStreamAsync(
        StreamId streamId,
        CancellationToken cancellationToken)
    {
        var rows = await dbContext.Events
            .AsNoTracking()
            .Where(storedEvent => storedEvent.StreamId == streamId.Value)
            .OrderBy(storedEvent => storedEvent.Version)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return rows.Select(ToEnvelope).ToArray();
    }

    private async Task<StreamVersion> CurrentVersionAsync(StreamId streamId, CancellationToken cancellationToken)
    {
        var maxVersion = await dbContext.Events
            .AsNoTracking()
            .Where(storedEvent => storedEvent.StreamId == streamId.Value)
            .Select(storedEvent => (int?)storedEvent.Version)
            .MaxAsync(cancellationToken)
            .ConfigureAwait(false);

        return StreamVersion.From(maxVersion ?? 0);
    }

    private EventEnvelope ToEnvelope(StoredEvent storedEvent)
    {
        var @event = serializer.Deserialize(storedEvent.EventType, storedEvent.Payload);
        var metadata = JsonSerializer.Deserialize<EventMetadata>(storedEvent.Metadata, metadataOptions)
            ?? EventMetadata.None;

        return new EventEnvelope(
            @event,
            StreamId.From(storedEvent.StreamId),
            StreamVersion.From(storedEvent.Version),
            storedEvent.GlobalSequence,
            metadata,
            storedEvent.OccurredOnUtc);
    }

    private static bool IsUniqueViolation(DbUpdateException exception)
    {
        // Npgsql surfaces a 23505 unique_violation as the inner PostgresException. Matched by the
        // provider-agnostic SQLSTATE rather than a typed reference so this assembly need not bind to
        // Npgsql exception types; the unique (stream_id, version) index is what was violated. This
        // true-race branch is exercised by EventStoreConcurrencyRaceTests against a real Postgres (#67) —
        // the sequential in-code version check cannot emit SQLSTATE 23505.
        for (var inner = exception.InnerException; inner is not null; inner = inner.InnerException)
        {
            var sqlState = inner.GetType().GetProperty("SqlState")?.GetValue(inner) as string;
            if (sqlState == "23505")
            {
                return true;
            }
        }

        return false;
    }
}
