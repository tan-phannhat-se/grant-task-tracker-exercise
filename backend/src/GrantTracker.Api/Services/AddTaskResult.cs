using GrantTracker.Api.Domain;

namespace GrantTracker.Api.Services;

/// <summary>
/// The task "add task" returns, and whether an earlier request with the same key had already added it.
/// </summary>
public record AddTaskResult(GrantTask Task, bool IsRetry);
