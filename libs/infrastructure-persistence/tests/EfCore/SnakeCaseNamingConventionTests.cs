using SmartSolutionsLab.Roomy.Infrastructure.Persistence.EfCore;

namespace SmartSolutionsLab.Roomy.Infrastructure.Persistence.Tests.EfCore;

public sealed class SnakeCaseNamingConventionTests
{
    [Theory]
    [InlineData("StreamId", "stream_id")]
    [InlineData("GlobalSequence", "global_sequence")]
    [InlineData("Events", "events")]
    [InlineData("OutboxMessages", "outbox_messages")]
    [InlineData("OccurredOnUtc", "occurred_on_utc")]
    [InlineData("ID", "id")]
    [InlineData("HTTPServer", "http_server")]
    public void Converts_pascal_case_to_snake_case(string input, string expected)
    {
        Assert.Equal(expected, SnakeCaseNamingConvention.ToSnakeCase(input));
    }

    [Fact]
    public void Leaves_existing_underscores_intact()
    {
        Assert.Equal("ux_events_stream_id_version", SnakeCaseNamingConvention.ToSnakeCase("UX_Events_StreamId_Version"));
    }

    [Fact]
    public void Throws_for_an_empty_name()
    {
        Assert.Throws<ArgumentException>(() => SnakeCaseNamingConvention.ToSnakeCase(string.Empty));
    }
}
