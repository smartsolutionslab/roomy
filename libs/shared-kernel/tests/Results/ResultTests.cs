using SmartSolutionsLab.Roomy.SharedKernel.Results;

namespace SmartSolutionsLab.Roomy.SharedKernel.Tests.Results;

public class ResultTests
{
    [Fact]
    public void Success_carries_the_value()
    {
        Result<int> result = 42;

        Assert.True(result.IsSuccess);
        Assert.Equal(42, result.Value);
    }

    [Fact]
    public void Failure_carries_the_error()
    {
        Result<int> result = Error.NotFound("desk.not_found", "Desk not found.");

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
    }

    [Fact]
    public void Accessing_value_on_failure_throws()
    {
        Result<int> result = Error.Conflict("desk.taken", "Desk already booked.");

        Assert.Throws<InvalidOperationException>(() => result.Value);
    }

    [Fact]
    public void Match_selects_the_branch()
    {
        Result<int> success = 10;
        Result<int> failure = Error.Validation("value.invalid", "bad");

        Assert.Equal("ok", success.Match(_ => "ok", _ => "err"));
        Assert.Equal("err", failure.Match(_ => "ok", _ => "err"));
    }
}
