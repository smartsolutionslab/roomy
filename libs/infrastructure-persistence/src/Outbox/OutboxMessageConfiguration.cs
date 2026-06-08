using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace SmartSolutionsLab.Roomy.Infrastructure.Persistence.Outbox;

/// <summary>
/// EF Core mapping for <see cref="OutboxMessage"/>. The payload is a Postgres <c>jsonb</c> column;
/// a partial-style index over unprocessed rows keeps the relay's "fetch pending" query cheap.
/// </summary>
public sealed class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    /// <summary>The table name (pre-snake_case) for the outbox.</summary>
    public const string TableName = "OutboxMessages";

    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable(TableName);

        builder.HasKey(message => message.Id);

        builder.Property(message => message.Id)
            .ValueGeneratedNever();

        builder.Property(message => message.Type)
            .IsRequired();

        builder.Property(message => message.Payload)
            .HasColumnType("jsonb")
            .IsRequired();

        builder.Property(message => message.OccurredOnUtc)
            .IsRequired();

        builder.HasIndex(message => message.ProcessedOnUtc)
            .HasDatabaseName("ix_outbox_messages_unprocessed");
    }
}
