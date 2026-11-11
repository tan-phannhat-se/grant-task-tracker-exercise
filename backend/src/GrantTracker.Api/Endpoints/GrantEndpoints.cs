using GrantTracker.Api.Domain;
using GrantTracker.Api.Endpoints.Requests;
using GrantTracker.Api.Endpoints.Responses;
using GrantTracker.Api.Services;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

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
        var grants = await grantService.ListGrantsAsync(
            GrantStatusFilter.Parse(status, whenMissing: null, field: "status"));
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

    private static async Task<GrantDetailResponse> CloseGrant(
        int id,
        GrantService grantService,
        ConfiguredTimeZoneCalendar calendar)
    {
        var grant = await grantService.CloseGrantAsync(id);
        return GrantDetailResponse.From(grant, calendar.Today());
    }

    private static async Task<Results<Created<TaskResponse>, Ok<TaskResponse>>> AddTask(
        int id,
        AddTaskRequest request,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        GrantService grantService,
        ConfiguredTimeZoneCalendar calendar)
    {
        // A missing key is allowed (no duplicate protection); a key that is sent must be a UUID.
        if (idempotencyKey is not null && !Guid.TryParseExact(idempotencyKey, "D", out _))
        {
            throw new DomainRuleException(
                ErrorCodes.IdempotencyKeyInvalid,
                DomainErrorKind.Validation,
                "Idempotency-Key must be a UUID, e.g. 3f2b6c1e-8d4a-4f0b-9c7e-2a1d5e6f7a8b.");
        }

        // The domain has no stand-in for a missing date, so it is reported here.
        var dueDate = request.DueDate ?? throw new DomainRuleException(
            ErrorCodes.DueDateRequired, DomainErrorKind.Validation, "Due date is required.", field: "dueDate");

        // A missing title goes in as empty and a missing priority as 0; the domain refuses both,
        // after checking the grant is open, so a closed grant still reports grant-closed first.
        var result = await grantService.AddTaskAsync(
            id, request.Title ?? string.Empty, dueDate, request.Priority ?? default, idempotencyKey);
        var response = TaskResponse.From(result.Task, calendar.Today());

        // A retry gets 200 with the task the first request added; a new task gets 201,
        // without Location: the brief has no GET /tasks/{id} to point at.
        return result.IsRetry
            ? TypedResults.Ok(response)
            : TypedResults.Created((string?)null, response);
    }
}
