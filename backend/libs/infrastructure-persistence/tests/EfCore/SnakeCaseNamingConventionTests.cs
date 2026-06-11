using Shouldly;
using SmartSolutionsLab.Roomy.Infrastructure.Persistence.EfCore;

namespace SmartSolutionsLab.Roomy.Infrastructure.Persistence.Tests.EfCore;

public sealed class SnakeCaseNamingConventionTests
{
    [Theory]
    [InlineData("StreamId", "stream_id")]
    [InlineData("GlobalSequence", "global_sequence")]
    [InlineData("Events", "events")]
    [InlineData("OccurredOnUtc", "occurred_on_utc")]
    [InlineData("ID", "id")]
    [InlineData("HTTPServer", "http_server")]
    public void Converts_pascal_case_to_snake_case(string input, string expected)
    {
        SnakeCaseNamingConvention.ToSnakeCase(input).ShouldBe(expected);
    }

    [Fact]
    public void Leaves_existing_underscores_intact()
    {
        SnakeCaseNamingConvention.ToSnakeCase("UX_Events_StreamId_Version").ShouldBe("ux_events_stream_id_version");
    }

    [Fact]
    public void Throws_for_an_empty_name()
    {
        Should.Throw<ArgumentException>(() => SnakeCaseNamingConvention.ToSnakeCase(string.Empty));
    }
}
