using UAM.Apis.Routes;
using UAM.Security;

namespace UAM.Apis;

public static class ApiRegister
{
    public static WebApplication RegisterApis(this WebApplication app)
    {
        var api = app.MapGroup("/api/v1")
            .WithSummary("API for UAM")
            .RequireAuthorization([SecurityConstants.ApiAccessPolicy]);

        api.RegisterAppRoutes();
        api.MapGroup("/stories").RegisterStoryRoutes().WithTags("Stories").WithSummary("Stories API");

        return app;
    }
}
