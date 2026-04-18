using Slogtry.Api;
using Slogtry.Security;
using UAM.Apis.Routes;

namespace UAM.Apis.Modules;

public sealed class UsersApiModule : IApiModule
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapGroup("/api/v1/users")
            .WithTags("Users")
            .WithSummary("Users API")
            .RequireAuthorization(SecurityConstants.ApiAccessPolicy)
            .RegisterUserRoutes();
    }
}
