namespace GrantTracker.Api.Endpoints;

/// <summary>
/// Which URL goes to which /tasks handler.
/// </summary>
public static partial class TaskEndpoints
{
    /// <summary>
    /// Maps every /tasks route.
    /// </summary>
    public static void MapTaskEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/tasks/overdue", ListOverdueTasks);
        app.MapPut("/tasks/{id:int}/complete", CompleteTask);
    }
}
