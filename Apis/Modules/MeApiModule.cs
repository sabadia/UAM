using Slogtry.Api;
using Slogtry.Security;
using UAM.Apis.Routes;

namespace UAM.Apis.Modules;

public sealed class MeApiModule : IApiModule
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapGroup("/api/v1")
            .WithTags("Users")
            .WithSummary("Current user API")
            .RequireAuthorization(SecurityConstants.ApiAccessPolicy)
            .RegisterMeRoutes();
    }
}
