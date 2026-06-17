using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Shouldly;
using SmartSolutionsLab.Roomy.SharedKernel.Results;

namespace SmartSolutionsLab.Roomy.Web.Http.Tests;

public class ResultResultsTests
{
    [Fact]
    public void ToOk_maps_a_success_value_to_a_200_with_the_projected_body()
    {
        var result = Result.Success(7).ToOk(value => value.ToString());

        var ok = result.ShouldBeOfType<Ok<string>>();
        ok.StatusCode.ShouldBe(StatusCodes.Status200OK);
        ok.Value.ShouldBe("7");
    }

    [Fact]
    public void ToOk_maps_a_failure_through_the_shared_http_mapping_by_default()
    {
        var result = Result.Failure<int>(Error.NotFound("user_not_found", "No such user.")).ToOk(value => value.ToString());

        var json = result.ShouldBeOfType<JsonHttpResult<ErrorResponse>>();
        json.StatusCode.ShouldBe(StatusCodes.Status404NotFound);
        json.Value!.Code.ShouldBe("user_not_found");
    }

    [Fact]
    public void ToOk_routes_a_failure_through_a_supplied_error_mapping()
    {
        var result = Result.Failure<int>(Error.Validation("bad_cursor", "Malformed cursor.")).ToOk(value => value.ToString(), ErrorResults.ToBadRequest);

        result.ShouldBeOfType<JsonHttpResult<ErrorResponse>>().StatusCode.ShouldBe(StatusCodes.Status400BadRequest);
    }

    [Fact]
    public void ToNoContent_maps_a_success_to_a_204()
    {
        var result = Result.Success().ToNoContent();

        result.ShouldBeOfType<NoContent>().StatusCode.ShouldBe(StatusCodes.Status204NoContent);
    }

    [Fact]
    public void ToNoContent_maps_a_failure_through_the_shared_http_mapping()
    {
        var result = Result.Failure(Error.Conflict("already_admin", "Already an administrator.")).ToNoContent();

        result.ShouldBeOfType<JsonHttpResult<ErrorResponse>>().StatusCode.ShouldBe(StatusCodes.Status409Conflict);
    }

    [Fact]
    public void ToCreated_maps_a_success_to_a_201_with_the_location_and_projected_body()
    {
        var result = Result.Success(7).ToCreated(value => $"/things/{value}", value => value.ToString());

        var created = result.ShouldBeOfType<Created<string>>();
        created.StatusCode.ShouldBe(StatusCodes.Status201Created);
        created.Location.ShouldBe("/things/7");
        created.Value.ShouldBe("7");
    }

    [Fact]
    public void ToCreated_maps_a_failure_through_the_shared_http_mapping()
    {
        var result = Result.Failure<int>(Error.Conflict("room_exists", "Room already exists.")).ToCreated(value => $"/things/{value}", value => value.ToString());

        result.ShouldBeOfType<JsonHttpResult<ErrorResponse>>().StatusCode.ShouldBe(StatusCodes.Status409Conflict);
    }
}
