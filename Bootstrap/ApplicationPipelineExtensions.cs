using Scalar.AspNetCore;
using UAM.Apis;
using UAM.Security;
using UAM.Services.Users;

namespace UAM.Bootstrap;

public static class ApplicationPipelineExtensions
{
    public static WebApplication UseUAMPipeline(this WebApplication app)
    {
        app.UseExceptionHandler();

        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
            app.MapScalarApiReference("/").AllowAnonymous();
        }

        app.UseAuthentication();
        app.UseTenantValidation();
        app.UseAuthorization();
        app.RegisterApis();
        app.MapGrpcService<UserAccessGrpcService>().RequireAuthorization([SecurityConstants.ApiAccessPolicy]);

        return app;
    }
}
