namespace GrantTracker.Api.Domain;

/// <summary>
/// What kind of rule failure an error is. The API turns it into a status code.
/// </summary>
public enum DomainErrorKind
{
    Validation = 100, // bad input, the caller can fix it → 400
    RuleRefused = 200, // the input is fine, the current state forbids it → 409
}
