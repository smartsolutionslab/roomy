using Shouldly;
using SmartSolutionsLab.Roomy.SharedKernel.Pagination;

namespace SmartSolutionsLab.Roomy.SharedKernel.Tests.Pagination;

public class CursorCodecTests
{
    private sealed record SampleKey(string Name, Guid Id);

    [Fact]
    public void Encode_then_decode_round_trips_the_key()
    {
        var key = new SampleKey("Ada", Guid.Parse("0193b9b0-0000-7000-8000-000000000001"));

        var cursor = CursorCodec.Encode(key);
        var decoded = CursorCodec.TryDecode<SampleKey>(cursor, out var result);

        decoded.ShouldBeTrue();
        result.ShouldBe(key);
    }

    [Fact]
    public void Encoded_cursor_is_url_safe_and_unpadded()
    {
        var cursor = CursorCodec.Encode(new SampleKey("a name with spaces & symbols", Guid.NewGuid()));

        cursor.ShouldNotContain('+');
        cursor.ShouldNotContain('/');
        cursor.ShouldNotContain('=');
    }

    [Fact]
    public void TryDecode_returns_false_on_a_non_base64_cursor()
    {
        CursorCodec.TryDecode<SampleKey>("not a cursor!!", out _).ShouldBeFalse();
    }

    [Fact]
    public void TryDecode_returns_false_on_base64_that_is_not_the_expected_shape()
    {
        var garbage = CursorCodec.Encode("just a string, not a SampleKey");

        CursorCodec.TryDecode<SampleKey>(garbage, out _).ShouldBeFalse();
    }
}
