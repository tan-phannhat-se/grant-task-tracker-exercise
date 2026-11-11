namespace GrantTracker.Api.Endpoints;

/// <summary>
/// Which URL goes to which /grants handler.
/// </summary>
public static partial class GrantEndpoints
{
    /// <summary>
    /// Maps every /grants route.
    /// </summary>
    public static void MapGrantEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/grants", ListGrants);
        app.MapGet("/grants/{id:int}", GetGrant);
        app.MapPost("/grants/{id:int}/tasks", AddTask);
        app.MapPut("/grants/{id:int}/close", CloseGrant);
    }
}
