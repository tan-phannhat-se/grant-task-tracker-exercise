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
    }
}
