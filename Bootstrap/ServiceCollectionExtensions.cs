using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using UAM.Context;
using UAM.Repositories;
using UAM.Security;
using System.Text.Json;
using System.Text.Json.Serialization;
using Identity.Grpc.Identity.V1;
using Tenant.Grpc.Tenancy.V1;
using UAM.Services.Users;

namespace UAM.Bootstrap;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddUamServices(this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        services.AddOpenApi();
        services.AddGrpc();

        services.AddProblemDetails();

        // Keep endpoint JSON payloads consistent across app and tests.
        services.ConfigureHttpJsonOptions(options =>
        {
            options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
            options.SerializerOptions.Converters.Add(new JsonStringEnumConverter(LowerCaseJsonNamingPolicy.Instance, false));
        });

        services.AddHttpContextAccessor();
        services.AddScoped<ITenantContextAccessor, TenantContextAccessor>();
        services.AddScoped<ITenantProvider, HttpContextTenantProvider>();
        services.AddScoped(typeof(IRepository<>), typeof(EfRepository<>));

        services.AddScoped<IUserService, UserService>();

        var jwtOptions = configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
                         ?? throw new InvalidOperationException($"Missing configuration section '{JwtOptions.SectionName}'.");

        if (string.IsNullOrWhiteSpace(jwtOptions.Issuer))
            throw new InvalidOperationException($"'{JwtOptions.SectionName}:Issuer' must be configured.");

        if (string.IsNullOrWhiteSpace(jwtOptions.Audience))
            throw new InvalidOperationException($"'{JwtOptions.SectionName}:Audience' must be configured.");

        services.AddSingleton(jwtOptions);
        services.AddMemoryCache();
        services.AddScoped<ITenantSigningKeyProvider, TenantSigningKeyProvider>();

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddScheme<AuthenticationSchemeOptions, TenantJwtAuthenticationHandler>(
                JwtBearerDefaults.AuthenticationScheme,
                _ => { });

        services.AddAuthorizationBuilder()
            .AddPolicy(SecurityConstants.ApiAccessPolicy, policy =>
                policy.RequireAuthenticatedUser());

        var remoteServicesOptions = configuration.GetSection(RemoteServicesOptions.SectionName).Get<RemoteServicesOptions>()
                                    ?? throw new InvalidOperationException($"Missing configuration section '{RemoteServicesOptions.SectionName}'.");

        if (string.IsNullOrWhiteSpace(remoteServicesOptions.TenantGrpcEndpoint))
            throw new InvalidOperationException($"'{RemoteServicesOptions.SectionName}:TenantGrpcEndpoint' must be configured.");

        if (string.IsNullOrWhiteSpace(remoteServicesOptions.IdentityGrpcEndpoint))
            throw new InvalidOperationException($"'{RemoteServicesOptions.SectionName}:IdentityGrpcEndpoint' must be configured.");

        services.AddSingleton(remoteServicesOptions);
        services.AddGrpcClient<TenantDirectory.TenantDirectoryClient>(options => { options.Address = new Uri(remoteServicesOptions.TenantGrpcEndpoint); });
        services.AddGrpcClient<IdentityAccess.IdentityAccessClient>(options => { options.Address = new Uri(remoteServicesOptions.IdentityGrpcEndpoint); });
        services.AddScoped<ITenantDirectoryClient, TenantDirectoryGrpcClient>();
        services.AddScoped<IIdentityAccessClient, IdentityAccessGrpcClient>();

        var connectionString = configuration.GetConnectionString("DefaultConnection")
                               ?? throw new InvalidOperationException("Missing connection string 'DefaultConnection'. Configure it with user-secrets or an environment variable.");

        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(connectionString, npgsqlOptions => npgsqlOptions.EnableRetryOnFailure()));

        if (environment.IsDevelopment()) services.AddDatabaseDeveloperPageExceptionFilter();

        return services;
    }
}
