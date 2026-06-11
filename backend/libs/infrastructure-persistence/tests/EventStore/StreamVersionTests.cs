using Shouldly;
using SmartSolutionsLab.Roomy.Infrastructure.Persistence.EventStore;

namespace SmartSolutionsLab.Roomy.Infrastructure.Persistence.Tests.EventStore;

public sealed class StreamVersionTests
{
    [Fact]
    public void None_is_zero()
    {
        StreamVersion.None.Value.ShouldBe(0);
    }

    [Fact]
    public void From_accepts_zero()
    {
        StreamVersion.From(0).Value.ShouldBe(0);
    }

    [Fact]
    public void From_rejects_negative()
    {
        Should.Throw<ArgumentException>(() => StreamVersion.From(-1));
    }

    [Fact]
    public void Next_advances_by_one()
    {
        StreamVersion.None.Next().Value.ShouldBe(1);
        StreamVersion.From(4).Next().Value.ShouldBe(5);
    }

    [Fact]
    public void Equal_versions_compare_equal()
    {
        StreamVersion.From(3).ShouldBe(StreamVersion.From(3));
        StreamVersion.From(3).ShouldNotBe(StreamVersion.From(4));
    }
}
