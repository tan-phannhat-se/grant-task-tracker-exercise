using GrantTracker.Api.Domain;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

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
    /// Writes a 400, 404 or 409 ProblemDetails for a domain rule error; leaves any other exception unhandled.
    /// </summary>
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is not DomainRuleException error)
        {
            return false; // a crash is not the user's fault, so it gets the default 500
        }

        var status = error.Kind switch
        {
            DomainErrorKind.Validation => StatusCodes.Status400BadRequest,
            DomainErrorKind.RuleRefused => StatusCodes.Status409Conflict,
            DomainErrorKind.NotFound => StatusCodes.Status404NotFound,
            _ => StatusCodes.Status500InternalServerError, // a new kind without a status yet
        };

        var problem = new ProblemDetails { Status = status, Detail = error.Message };
        problem.Extensions["code"] = error.Code;
        if (error.Field is not null)
        {
            problem.Extensions["field"] = error.Field;
        }

        httpContext.Response.StatusCode = status;
        return await _problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = problem,
            Exception = exception,
        });
    }
}
