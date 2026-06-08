using Shouldly;
using SmartSolutionsLab.Roomy.SharedKernel.Results;

namespace SmartSolutionsLab.Roomy.SharedKernel.Tests.Results;

public class ResultTests
{
    [Fact]
    public void Success_carries_the_value()
    {
        Result<int> result = 42;

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(42);
    }

    [Fact]
    public void Failure_carries_the_error()
    {
        Result<int> result = Error.NotFound("desk.not_found", "Desk not found.");

        result.IsFailure.ShouldBeTrue();
        result.Error.Type.ShouldBe(ErrorType.NotFound);
    }

    [Fact]
    public void Accessing_value_on_failure_throws()
    {
        Result<int> result = Error.Conflict("desk.taken", "Desk already booked.");

        Should.Throw<InvalidOperationException>(() => result.Value);
    }

    [Fact]
    public void Match_selects_the_branch()
    {
        Result<int> success = 10;
        Result<int> failure = Error.Validation("value.invalid", "bad");

        success.Match(_ => "ok", _ => "err").ShouldBe("ok");
        failure.Match(_ => "ok", _ => "err").ShouldBe("err");
    }
}
