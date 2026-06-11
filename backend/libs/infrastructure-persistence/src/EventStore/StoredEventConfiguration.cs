using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace SmartSolutionsLab.Roomy.Infrastructure.Persistence.EventStore;

public sealed class StoredEventConfiguration : IEntityTypeConfiguration<StoredEvent>
{
    public const string TableName = "Events";

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
