using Microsoft.AspNetCore.Diagnostics;

namespace GrantTracker.Api.Endpoints;

/// <summary>
/// Turns a broken business rule into ProblemDetails, in one place for every endpoint.
/// </summary>
public sealed class DomainRuleExceptionHandler : IExceptionHandler
{
    private readonly IProblemDetailsService _problemDetailsService;

    /// <summary>
    /// Creates the handler with the service that writes ProblemDetails responses.
    /// </summary>
    public DomainRuleExceptionHandler(IProblemDetailsService problemDetailsService)
    {
        _problemDetailsService = problemDetailsService;
    }

    /// <summary>
    /// Writes a 400 or 409 ProblemDetails for a domain rule error; leaves any other exception unhandled.
    /// </summary>
    public ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken) => throw new NotImplementedException();
}
