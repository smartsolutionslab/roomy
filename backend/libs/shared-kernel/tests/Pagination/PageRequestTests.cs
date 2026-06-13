using Shouldly;
using SmartSolutionsLab.Roomy.SharedKernel.Pagination;

namespace SmartSolutionsLab.Roomy.SharedKernel.Tests.Pagination;

public class PageRequestTests
{
    private sealed record SampleKey(string Name, Guid Id);

    [Fact]
    public void Absent_limit_defaults_to_fifty()
    {
        var request = PageRequest.From(cursor: null, limit: null);

        request.Limit.ShouldBe(PageRequest.DefaultLimit);
        request.Limit.ShouldBe(50);
    }

    [Fact]
    public void A_limit_within_range_is_honoured()
    {
        PageRequest.From(cursor: null, limit: 25).Limit.ShouldBe(25);
    }

    [Fact]
    public void A_limit_above_the_maximum_is_out_of_range()
    {
        Should.Throw<ArgumentOutOfRangeException>(() => PageRequest.From(cursor: null, limit: PageRequest.MaxLimit + 1));
    }

    [Fact]
    public void A_limit_below_one_is_out_of_range()
    {
        Should.Throw<ArgumentOutOfRangeException>(() => PageRequest.From(cursor: null, limit: 0));
        Should.Throw<ArgumentOutOfRangeException>(() => PageRequest.From(cursor: null, limit: -3));
    }

    [Fact]
    public void Blank_cursor_is_treated_as_the_first_page()
    {
        PageRequest.From(cursor: "   ", limit: null).Cursor.ShouldBeNull();
    }

    [Fact]
    public void Decoding_an_absent_cursor_yields_a_null_key()
    {
        var request = PageRequest.From(cursor: null, limit: null);

        request.DecodeCursor<SampleKey>().ShouldBeNull();
    }

    [Fact]
    public void Decoding_a_valid_cursor_yields_the_key()
    {
        var key = new SampleKey("Ada", Guid.NewGuid());
        var request = PageRequest.From(CursorCodec.Encode(key), limit: null);

        request.DecodeCursor<SampleKey>().ShouldBe(key);
    }

    [Fact]
    public void Decoding_a_malformed_cursor_throws()
    {
        var request = PageRequest.From(cursor: "not-a-cursor", limit: null);

        Should.Throw<ArgumentException>(() => request.DecodeCursor<SampleKey>());
    }
}
