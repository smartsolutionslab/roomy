using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Shouldly;

namespace SmartSolutionsLab.Roomy.Web.Http.Tests;

public class ArgumentExceptionHandlerTests
{
    private readonly ArgumentExceptionHandler handler = new();

    [Fact]
    public async Task An_argument_exception_is_written_as_a_400_with_the_message()
    {
        var context = ContextWithBodyBuffer();

        var handled = await handler.TryHandleAsync(context, new ArgumentException("WorkEmail must be a valid email address.", "value"), TestContext.Current.CancellationToken);

        handled.ShouldBeTrue();
        context.Response.StatusCode.ShouldBe(StatusCodes.Status400BadRequest);

        var body = await ReadBodyAsync(context);
        body.GetProperty("code").GetString().ShouldBe("bad_request");
        body.GetProperty("message").GetString().ShouldBe("WorkEmail must be a valid email address. (Parameter 'value')");
    }

    [Fact]
    public async Task An_argument_out_of_range_exception_is_also_written_as_a_400()
    {
        var context = ContextWithBodyBuffer();

        var handled = await handler.TryHandleAsync(context, new ArgumentOutOfRangeException("limit", "The page limit must be between 1 and 100."), TestContext.Current.CancellationToken);

        handled.ShouldBeTrue();
        context.Response.StatusCode.ShouldBe(StatusCodes.Status400BadRequest);
    }

    [Fact]
    public async Task Any_other_exception_is_left_unhandled()
    {
        var context = ContextWithBodyBuffer();

        var handled = await handler.TryHandleAsync(context, new InvalidOperationException("boom"), TestContext.Current.CancellationToken);

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
