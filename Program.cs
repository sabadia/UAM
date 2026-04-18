using Slogtry.Security;
using Slogtry.ServiceDefaults;
using Scalar.AspNetCore;
using UAM.Apis;
using UAM.Context;
using UAM.Middleware;
using UAM.Services.Users;

var builder = WebApplication.CreateBuilder(args);
ConfigureServices(builder);

var app = builder.Build();
ConfigurePipeline(app);

await app.RunAsync();

public partial class Program
{
    public static void ConfigureServices(WebApplicationBuilder builder)
    {
        builder.AddSlogtryServiceDefaults();
        builder.AddSlogtryJwtAuth();
        builder.AddSlogtryDatabase<AppDbContext>();
        builder.AddSlogtryRepositories();
        builder.AddRemoteGrpcClient<
            Tenant.Grpc.Tenancy.V1.TenantDirectory.TenantDirectoryClient,
            ITenantDirectoryClient,
            Slogtry.Contracts.Tenant.TenantDirectoryGrpcClient>("Tenant");
        builder.AddRemoteGrpcClient<
            Identity.Grpc.Identity.V1.IdentityAccess.IdentityAccessClient,
            IIdentityAccessClient,
            Slogtry.Contracts.Identity.IdentityAccessGrpcClient>("Identity");
        builder.Services.AddGrpc();
        builder.Services.AddScoped<IUserService, UserService>();
    }

    public static void ConfigurePipeline(WebApplication app)
    {
        app.UseExceptionHandler();

        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
            app.MapScalarApiReference("/").AllowAnonymous();
        }

        app.UseAuthentication();
        app.UseUamTenantValidation();
        app.UseAuthorization();
        app.RegisterApis();
        app.MapGrpcService<UserAccessGrpcService>().RequireAuthorization([SecurityConstants.ApiAccessPolicy]);
    }
}
