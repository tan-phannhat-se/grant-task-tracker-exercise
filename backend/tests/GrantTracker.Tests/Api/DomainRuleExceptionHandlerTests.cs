using System.Text.Json;
using GrantTracker.Api.Domain;
using GrantTracker.Api.Endpoints;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace GrantTracker.Tests.Api;

/// <summary>
/// A domain rule error becomes ProblemDetails with the right status, code and field; other errors are left alone.
/// </summary>
public class DomainRuleExceptionHandlerTests
{
    /// <summary>
    /// PD-1: a Validation error with a field → handled as 400 with code, field and the message as detail.
    /// Catches: a wrong status for Validation, or code or field missing from the body.
    /// </summary>
    [Fact]
    public async Task TryHandle_ValidationError_Writes400WithCodeAndField()
    {
        var error = new DomainRuleException(
            ErrorCodes.TitleRequired, DomainErrorKind.Validation, "Title is required.", field: "title");

        var (handled, status, body) = await Handle(error);

        Assert.True(handled);
        Assert.Equal(StatusCodes.Status400BadRequest, status);
        Assert.Equal("title-required", body!.Value.GetProperty("code").GetString());
        Assert.Equal("title", body.Value.GetProperty("field").GetString());
        Assert.Equal("Title is required.", body.Value.GetProperty("detail").GetString());
    }

    /// <summary>
    /// PD-2: a RuleRefused error without a field → handled as 409 with its code and no field.
    /// Catches: every domain error mapped to 400.
    /// </summary>
    [Fact]
    public async Task TryHandle_RuleRefusedError_Writes409WithCode()
    {
        var error = new DomainRuleException(
            ErrorCodes.GrantClosed, DomainErrorKind.RuleRefused, "Grant GR-100003 is closed.");

        var (handled, status, body) = await Handle(error);

        Assert.True(handled);
        Assert.Equal(StatusCodes.Status409Conflict, status);
        Assert.Equal("grant-closed", body!.Value.GetProperty("code").GetString());
        Assert.False(body.Value.TryGetProperty("field", out _));
    }

    /// <summary>
    /// PD-3: any other exception → not handled, so the default 500 ProblemDetails is written instead.
    /// Catches: a server crash reported as a 400 that blames the user's input.
    /// </summary>
    [Fact]
    public async Task TryHandle_OtherException_IsNotHandled()
    {
        var (handled, _, body) = await Handle(new InvalidOperationException("Database unavailable."));

        Assert.False(handled);
        Assert.Null(body);
    }

    private static async Task<(bool Handled, int Status, JsonElement? Body)> Handle(Exception exception)
    {
        var services = new ServiceCollection().AddLogging().AddProblemDetails().BuildServiceProvider();
        var context = new DefaultHttpContext { RequestServices = services };
        context.Response.Body = new MemoryStream();
        var handler = new DomainRuleExceptionHandler(services.GetRequiredService<IProblemDetailsService>());

        var handled = await handler.TryHandleAsync(context, exception, CancellationToken.None);

        context.Response.Body.Position = 0;
        JsonElement? body = context.Response.Body.Length == 0
            ? null
            : (await JsonDocument.ParseAsync(context.Response.Body)).RootElement;
        return (handled, context.Response.StatusCode, body);
    }
}
