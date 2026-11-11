using GrantTracker.Api.Domain;

namespace GrantTracker.Api.Endpoints.Requests;

/// <summary>
/// The body of "add a task". Every field may be missing in the JSON; the endpoint decides what that means.
/// </summary>
public record AddTaskRequest(string? Title, DateOnly? DueDate, Priority? Priority);
