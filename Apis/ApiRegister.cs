using UAM.Apis.Routes;

namespace UAM.Apis;

public static class ApiRegister
{
    public static WebApplication RegisterApis(this WebApplication app)
    {
        var api = app.MapGroup("/api/v1")
            .WithSummary("API for UAM")
            .RequireAuthorization([SecurityConstants.ApiAccessPolicy]);

        api.RegisterAppRoutes();
        api.MapGroup("/users").RegisterUserRoutes().WithTags("Users").WithSummary("Users API");
        api.RegisterMeRoutes().WithTags("Users").WithSummary("Current user API");

        return app;
    }
}
