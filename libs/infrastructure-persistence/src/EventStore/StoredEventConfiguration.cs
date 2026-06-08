using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace SmartSolutionsLab.Roomy.Infrastructure.Persistence.EventStore;

/// <summary>
/// EF Core mapping for the append-only events table (ADR-0012). The key load-bearing rules:
/// <list type="bullet">
/// <item><see cref="StoredEvent.GlobalSequence"/> is the <c>bigserial</c> primary key, giving a
/// monotonic global order for projections.</item>
/// <item>A <em>unique</em> index on <c>(stream_id, version)</c> enforces optimistic concurrency at
/// the database — two writers asserting the same expected version cannot both commit.</item>
/// <item>Payload and metadata are Postgres <c>jsonb</c>.</item>
/// </list>
/// </summary>
public sealed class StoredEventConfiguration : IEntityTypeConfiguration<StoredEvent>
{
    /// <summary>The table name (pre-snake_case) for the event log.</summary>
    public const string TableName = "Events";

    /// <summary>The name (pre-snake_case) of the unique <c>(stream_id, version)</c> index.</summary>
    public const string StreamVersionIndexName = "UX_Events_StreamId_Version";

    public void Configure(EntityTypeBuilder<StoredEvent> builder)
    {
        builder.ToTable(TableName);

        builder.HasKey(storedEvent => storedEvent.GlobalSequence);

        builder.Property(storedEvent => storedEvent.GlobalSequence)
            .UseIdentityByDefaultColumn();

        builder.Property(storedEvent => storedEvent.StreamId)
            .IsRequired();

        builder.Property(storedEvent => storedEvent.Version)
            .IsRequired();

        builder.Property(storedEvent => storedEvent.EventType)
            .IsRequired();

        builder.Property(storedEvent => storedEvent.Payload)
            .HasColumnType("jsonb")
            .IsRequired();

        builder.Property(storedEvent => storedEvent.Metadata)
            .HasColumnType("jsonb")
            .IsRequired();

        builder.Property(storedEvent => storedEvent.OccurredOnUtc)
            .IsRequired();

        builder.HasIndex(storedEvent => new { storedEvent.StreamId, storedEvent.Version })
            .IsUnique()
            .HasDatabaseName(StreamVersionIndexName);
    }
}
