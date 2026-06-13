using Shouldly;
using SmartSolutionsLab.Roomy.SharedKernel.Pagination;
using SmartSolutionsLab.Roomy.SharedKernel.Results;

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
    public void A_limit_above_the_maximum_throws_a_bad_request()
    {
        var exception = Should.Throw<BadRequestException>(() => PageRequest.From(cursor: null, limit: PageRequest.MaxLimit + 1));

        exception.Error.Code.ShouldBe("pagination.limit_out_of_range");
        exception.Error.Type.ShouldBe(ErrorType.Validation);
    }

    [Fact]
    public void A_limit_below_one_throws_a_bad_request()
    {
        Should.Throw<BadRequestException>(() => PageRequest.From(cursor: null, limit: 0));
        Should.Throw<BadRequestException>(() => PageRequest.From(cursor: null, limit: -3));
    }

    [Fact]
    public void Blank_cursor_is_treated_as_the_first_page()
    {
        PageRequest.From(cursor: "   ", limit: null).Cursor.ShouldBeNull();
    }

    [Fact]
    public void Decoding_an_absent_cursor_yields_a_null_key_and_no_error()
    {
        var request = PageRequest.From(cursor: null, limit: null);

        var decoded = request.DecodeCursor<SampleKey>();

        decoded.IsSuccess.ShouldBeTrue();
        decoded.Value.ShouldBeNull();
    }

    [Fact]
    public void Decoding_a_valid_cursor_yields_the_key()
    {
        var key = new SampleKey("Ada", Guid.NewGuid());
        var request = PageRequest.From(CursorCodec.Encode(key), limit: null);

        var decoded = request.DecodeCursor<SampleKey>();

        decoded.IsSuccess.ShouldBeTrue();
        decoded.Value.ShouldBe(key);
    }

    [Fact]
    public void Decoding_a_malformed_cursor_is_a_validation_error()
    {
        var request = PageRequest.From(cursor: "not-a-cursor", limit: null);

        var decoded = request.DecodeCursor<SampleKey>();

        decoded.IsFailure.ShouldBeTrue();
        decoded.Error.Type.ShouldBe(ErrorType.Validation);
    }
}
