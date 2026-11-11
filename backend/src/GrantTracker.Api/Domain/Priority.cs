namespace GrantTracker.Api.Domain;

/// <summary>
/// How urgent a task is. Values are gapped so a level can be added later.
/// </summary>
public enum Priority
{
    Low = 100,
    Medium = 200,
    High = 300,
}
