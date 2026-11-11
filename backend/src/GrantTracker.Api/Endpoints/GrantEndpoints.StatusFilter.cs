using GrantTracker.Api.Domain;

namespace GrantTracker.Api.Endpoints;

/// <summary>
/// Reads the ?status= filter of the grant list.
/// </summary>
public static partial class GrantEndpoints
{
    // Parsed by hand: binding to the enum would also accept numbers such as "100".
    private static GrantStatus? ParseStatusFilter(string? status)
    {
        switch (status?.ToLowerInvariant())
        {
            case null or "" or "all":
                return null;

            case "open":
                return GrantStatus.Open;

            case "closed":
                return GrantStatus.Closed;

            default:
                throw new DomainRuleException(
                    ErrorCodes.StatusInvalid,
                    DomainErrorKind.Validation,
                    "Status must be Open, Closed or All.",
                    field: "status");
        }
    }
}
