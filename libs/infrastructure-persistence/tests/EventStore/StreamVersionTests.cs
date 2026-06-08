using SmartSolutionsLab.Roomy.Infrastructure.Persistence.EventStore;

namespace SmartSolutionsLab.Roomy.Infrastructure.Persistence.Tests.EventStore;

public sealed class StreamVersionTests
{
    [Fact]
    public void None_is_zero()
    {
        Assert.Equal(0, StreamVersion.None.Value);
    }

    [Fact]
    public void From_accepts_zero()
    {
        Assert.Equal(0, StreamVersion.From(0).Value);
    }

    [Fact]
    public void From_rejects_negative()
    {
        Assert.Throws<ArgumentException>(() => StreamVersion.From(-1));
    }

    [Fact]
    public void Next_advances_by_one()
    {
        Assert.Equal(1, StreamVersion.None.Next().Value);
        Assert.Equal(5, StreamVersion.From(4).Next().Value);
    }

    [Fact]
    public void Equal_versions_compare_equal()
    {
        Assert.Equal(StreamVersion.From(3), StreamVersion.From(3));
        Assert.NotEqual(StreamVersion.From(3), StreamVersion.From(4));
    }
}
