namespace GrantTracker.Api.Domain;

/// <summary>
/// Thrown when a business rule is broken. One API handler turns it into ProblemDetails.
/// </summary>
public sealed class DomainRuleException : Exception
{
    public string Code { get; } // one of ErrorCodes
    public DomainErrorKind Kind { get; }
    public string? Field { get; } // the input the error belongs to, so the screen can show it there

    /// <summary>
    /// Creates the error with its code, kind and message.
    /// </summary>
    public DomainRuleException(string code, DomainErrorKind kind, string message, string? field = null)
        : base(message)
    {
        Code = code;
        Kind = kind;
        Field = field;
    }
}
