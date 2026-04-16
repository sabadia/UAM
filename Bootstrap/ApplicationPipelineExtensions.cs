using Scalar.AspNetCore;
using UAM.Apis;

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

        return app;
    }
}
