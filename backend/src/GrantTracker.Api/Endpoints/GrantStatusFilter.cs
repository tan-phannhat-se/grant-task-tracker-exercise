using GrantTracker.Api.Domain;

namespace GrantTracker.Api.Endpoints;

/// <summary>
/// Reads a grant status filter (All, Open or Closed) from the query string.
/// </summary>
public static class GrantStatusFilter
{
    /// <summary>
    /// Returns the status to filter on, or null for every grant.
    /// </summary>
    public static GrantStatus? Parse(string? value, GrantStatus? whenMissing, string field)
    {
        // Parsed by hand: binding to the enum would also accept numbers such as "100".
        switch (value?.ToLowerInvariant())
        {
            case null or "":
                return whenMissing;

            case "all":
                return null;

            case "open":
                return GrantStatus.Open;

            case "closed":
                return GrantStatus.Closed;

            default:
                throw new DomainRuleException(
                    ErrorCodes.StatusInvalid,
                    DomainErrorKind.Validation,
                    $"The {field} filter must be Open, Closed or All.",
                    field: field);
        }
    }
}
