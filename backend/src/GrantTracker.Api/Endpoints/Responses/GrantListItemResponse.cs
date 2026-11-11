using GrantTracker.Api.Domain;

namespace GrantTracker.Api.Endpoints.Responses;

/// <summary>
/// One row of the grant list.
/// </summary>
public record GrantListItemResponse(
    int Id,
    string ReferenceNumber,
    string OrganisationName,
    DateOnly ApplicationDate,
    GrantStatus Status,
    int PendingTaskCount)
{
    /// <summary>
    /// Builds the row from a grant loaded with its tasks.
    /// </summary>
    public static GrantListItemResponse From(Grant grant) => new(
        grant.Id,
        grant.ReferenceNumber,
        grant.OrganisationName,
        grant.ApplicationDate,
        grant.Status,
        grant.PendingTaskCount);
}
