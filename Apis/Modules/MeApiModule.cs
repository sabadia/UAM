using Slogtry.Api;
using Slogtry.Security;
using UAM.Apis.Routes;

namespace UAM.Apis.Modules;

public sealed class MeApiModule : IApiModule
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapVersionedApi()
            .WithTags("Users")
            .WithSummary("Current user API")
            .RequireAuthorization(SecurityConstants.ApiAccessPolicy)
            .RegisterMeRoutes();
    }
}
