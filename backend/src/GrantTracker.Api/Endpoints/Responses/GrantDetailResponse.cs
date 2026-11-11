using GrantTracker.Api.Domain;

namespace GrantTracker.Api.Endpoints.Responses;

/// <summary>
/// One grant with all its tasks.
/// </summary>
public record GrantDetailResponse(
    int Id,
    string ReferenceNumber,
    string OrganisationName,
    DateOnly ApplicationDate,
    GrantStatus Status,
    IReadOnlyList<TaskResponse> Tasks)
{
    /// <summary>
    /// Builds the response from a grant loaded with its tasks, most urgent task first.
    /// </summary>
    public static GrantDetailResponse From(Grant grant, DateOnly today) => new(
        grant.Id,
        grant.ReferenceNumber,
        grant.OrganisationName,
        grant.ApplicationDate,
        grant.Status,
        grant.Tasks
            .OrderBy(t => t.DueDate)
            .ThenBy(t => t.Id)
            .Select(t => TaskResponse.From(t, today))
            .ToList());
}
