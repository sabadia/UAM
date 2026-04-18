using Slogtry.Api;
using Slogtry.Security;
using UAM.Apis.Routes;

namespace UAM.Apis.Modules;

public sealed class AppApiModule : IApiModule
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapGroup("/api/v1")
            .RequireAuthorization(SecurityConstants.ApiAccessPolicy)
            .RegisterAppRoutes();
    }
}
