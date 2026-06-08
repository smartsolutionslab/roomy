using SmartSolutionsLab.Roomy.Infrastructure.Persistence.EventStore;

namespace SmartSolutionsLab.Roomy.Infrastructure.Persistence.Tests.EventStore;

public sealed class StreamIdTests
{
    [Fact]
    public void From_accepts_a_non_empty_guid()
    {
        var id = Guid.NewGuid();

        Assert.Equal(id, StreamId.From(id).Value);
    }

    [Fact]
    public void From_rejects_the_empty_guid()
    {
        Assert.Throws<ArgumentException>(() => StreamId.From(Guid.Empty));
    }

    [Fact]
    public void Same_guid_yields_equal_stream_ids()
    {
        var id = Guid.NewGuid();

        Assert.Equal(StreamId.From(id), StreamId.From(id));
    }
}
