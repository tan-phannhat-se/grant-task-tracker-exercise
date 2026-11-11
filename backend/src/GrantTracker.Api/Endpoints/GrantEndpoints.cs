using GrantTracker.Api.Domain;
using GrantTracker.Api.Endpoints.Requests;
using GrantTracker.Api.Endpoints.Responses;
using GrantTracker.Api.Services;
using Microsoft.AspNetCore.Http.HttpResults;

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

    private static async Task<Created<TaskResponse>> AddTask(
        int id,
        AddTaskRequest request,
        GrantService grantService,
        ConfiguredTimeZoneCalendar calendar)
    {
        // The domain has no stand-in for a missing date, so it is reported here.
        var dueDate = request.DueDate ?? throw new DomainRuleException(
            ErrorCodes.DueDateRequired, DomainErrorKind.Validation, "Due date is required.", field: "dueDate");

        // A missing title goes in as empty and a missing priority as 0; the domain refuses both,
        // after checking the grant is open, so a closed grant still reports grant-closed first.
        var task = await grantService.AddTaskAsync(
            id, request.Title ?? string.Empty, dueDate, request.Priority ?? default);

        // 201 without Location: the brief has no GET /tasks/{id} to point at.
        return TypedResults.Created((string?)null, TaskResponse.From(task, calendar.Today()));
    }
}
