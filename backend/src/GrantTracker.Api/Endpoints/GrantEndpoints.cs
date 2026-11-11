using GrantTracker.Api.Endpoints.Responses;
using GrantTracker.Api.Services;

namespace GrantTracker.Api.Endpoints;

/// <summary>
/// The /grants handlers: HTTP in and out only; the work is done by GrantService.
/// </summary>
public static partial class GrantEndpoints
{
    private static async Task<IReadOnlyList<GrantListItemResponse>> ListGrants(
        GrantService grantService,
        string? status)
    {
        var grants = await grantService.ListGrantsAsync(ParseStatusFilter(status));
        return grants.Select(GrantListItemResponse.From).ToList();
    }

    private static async Task<GrantDetailResponse> GetGrant(
        int id,
        GrantService grantService,
        ConfiguredTimeZoneCalendar calendar)
    {
        var grant = await grantService.GetGrantAsync(id);
        return GrantDetailResponse.From(grant, calendar.Today());
    }
}
