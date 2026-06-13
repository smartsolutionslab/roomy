using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Shouldly;
using SmartSolutionsLab.Roomy.SharedKernel.Results;

namespace SmartSolutionsLab.Roomy.Web.Http.Tests;

public class BadRequestExceptionHandlerTests
{
    private readonly BadRequestExceptionHandler handler = new();

    [Fact]
    public async Task A_bad_request_exception_is_written_as_a_400_with_the_error_body()
    {
        var context = ContextWithBodyBuffer();
        var exception = new BadRequestException(Error.Validation("pagination.limit_out_of_range", "The page limit must be between 1 and 100."));

        var handled = await handler.TryHandleAsync(context, exception, CancellationToken.None);

        handled.ShouldBeTrue();
        context.Response.StatusCode.ShouldBe(StatusCodes.Status400BadRequest);

        var body = await ReadBodyAsync(context);
        body.GetProperty("code").GetString().ShouldBe("pagination.limit_out_of_range");
        body.GetProperty("message").GetString().ShouldBe("The page limit must be between 1 and 100.");
    }

    [Fact]
    public async Task Any_other_exception_is_left_unhandled()
    {
        var context = ContextWithBodyBuffer();

        var handled = await handler.TryHandleAsync(context, new InvalidOperationException("boom"), CancellationToken.None);

        handled.ShouldBeFalse();
        context.Response.StatusCode.ShouldBe(StatusCodes.Status200OK);
    }

    private static DefaultHttpContext ContextWithBodyBuffer() =>
        new() { Response = { Body = new MemoryStream() } };

    private static async Task<JsonElement> ReadBodyAsync(HttpContext context)
    {
        context.Response.Body.Position = 0;
        using var reader = new StreamReader(context.Response.Body, Encoding.UTF8);
        var json = await reader.ReadToEndAsync();
        return JsonSerializer.Deserialize<JsonElement>(json);
    }
}
