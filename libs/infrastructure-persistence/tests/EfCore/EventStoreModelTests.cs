using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Shouldly;
using SmartSolutionsLab.Roomy.Infrastructure.Persistence.EventStore;
using SmartSolutionsLab.Roomy.Infrastructure.Persistence.Tests.EventStore;

namespace SmartSolutionsLab.Roomy.Infrastructure.Persistence.Tests.EfCore;

public sealed class EventStoreModelTests
{
    // The EF model (table/column names, keys, indexes) is built from configuration alone, so it needs no
    // live database — a context configured for Npgsql with an unused connection string is enough, keeping
    // these pure-metadata assertions Docker-free.
    private static IModel BuildModel()
    {
        using var context = new TestEventStoreDbContext(
            new DbContextOptionsBuilder<TestEventStoreDbContext>()
                .UseNpgsql("Host=unused;Database=unused;Username=unused;Password=unused")
                .Options);
        return context.Model;
    }

    [Fact]
    public void Events_table_is_snake_cased()
    {
        var entity = BuildModel().FindEntityType(typeof(StoredEvent));

        entity!.GetTableName().ShouldBe("events");
    }

    [Fact]
    public void Stored_event_columns_are_snake_cased()
    {
        var entity = BuildModel().FindEntityType(typeof(StoredEvent))!;

        var columnNames = entity.GetProperties().Select(property => property.GetColumnName()).ToArray();
        columnNames.ShouldContain("stream_id");
        columnNames.ShouldContain("global_sequence");
        columnNames.ShouldContain("occurred_on_utc");
    }

    [Fact]
    public void Events_table_is_keyed_by_the_global_sequence()
    {
        var entity = BuildModel().FindEntityType(typeof(StoredEvent))!;

        var primaryKey = entity.FindPrimaryKey()!;

        primaryKey.Properties.ShouldHaveSingleItem().Name.ShouldBe(nameof(StoredEvent.GlobalSequence));
    }

    [Fact]
    public void Stream_id_and_version_have_a_unique_index()
    {
        var entity = BuildModel().FindEntityType(typeof(StoredEvent))!;

        var index = entity.GetIndexes().Single(candidate =>
            candidate.Properties.Select(property => property.Name)
                .SequenceEqual([nameof(StoredEvent.StreamId), nameof(StoredEvent.Version)]));

        index.IsUnique.ShouldBeTrue();
    }
}
