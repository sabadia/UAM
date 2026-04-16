using Microsoft.AspNetCore.Http.HttpResults;

namespace UAM.Apis.Routes;

internal record HealthResult(string Status, DateTimeOffset Date);

public static class AppRoutes
{
    public static RouteGroupBuilder RegisterAppRoutes(this RouteGroupBuilder route)
    {
        route.MapGet("/health", HealthCheck)
            .WithName("HealthCheck")
            .WithSummary("Health Check")
            .WithDescription("Check the health of the application")
            .AllowAnonymous();

        return route;
    }

    private static Ok<HealthResult> HealthCheck()
    {
        return TypedResults.Ok(new HealthResult("Healthy", DateTimeOffset.UtcNow));
    }
}
