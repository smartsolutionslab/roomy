using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using SmartSolutionsLab.Roomy.Infrastructure.Persistence.EventStore;
using SmartSolutionsLab.Roomy.Infrastructure.Persistence.Tests.EventStore;

namespace SmartSolutionsLab.Roomy.Infrastructure.Persistence.Tests.EfCore;

/// <summary>
/// Asserts the EF model the baseline produces (ADR-0012): the events table exists and is
/// snake_cased, the events table is keyed by the global sequence, and the unique
/// <c>(stream_id, version)</c> index — the optimistic-concurrency guard — is present and unique.
/// </summary>
public sealed class EventStoreModelTests
{
    private static IModel BuildModel()
    {
        using var fixture = new SqliteEventStoreFixture();
        using var context = fixture.CreateContext();
        return context.Model;
    }

    [Fact]
    public void Events_table_is_snake_cased()
    {
        var entity = BuildModel().FindEntityType(typeof(StoredEvent));

        Assert.Equal("events", entity!.GetTableName());
    }

    [Fact]
    public void Stored_event_columns_are_snake_cased()
    {
        var entity = BuildModel().FindEntityType(typeof(StoredEvent))!;

        Assert.Contains("stream_id", entity.GetProperties().Select(p => p.GetColumnName()));
        Assert.Contains("global_sequence", entity.GetProperties().Select(p => p.GetColumnName()));
        Assert.Contains("occurred_on_utc", entity.GetProperties().Select(p => p.GetColumnName()));
    }

    [Fact]
    public void Events_table_is_keyed_by_the_global_sequence()
    {
        var entity = BuildModel().FindEntityType(typeof(StoredEvent))!;

        var primaryKey = entity.FindPrimaryKey()!;

        Assert.Equal(nameof(StoredEvent.GlobalSequence), Assert.Single(primaryKey.Properties).Name);
    }

    [Fact]
    public void Stream_id_and_version_have_a_unique_index()
    {
        var entity = BuildModel().FindEntityType(typeof(StoredEvent))!;

        var index = entity.GetIndexes().Single(i =>
            i.Properties.Select(p => p.Name).SequenceEqual([nameof(StoredEvent.StreamId), nameof(StoredEvent.Version)]));

        Assert.True(index.IsUnique);
    }
}
