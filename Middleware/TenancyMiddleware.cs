using System.Security.Claims;
using System.Text.RegularExpressions;
using Slogtry.Abstractions;
using Slogtry.Grpc;

namespace UAM.Middleware;

public static class TenancyMiddleware
{
    private static readonly Regex TenantIdPattern = new("^[a-zA-Z0-9][a-zA-Z0-9-]{1,63}$", RegexOptions.Compiled);

    public static IApplicationBuilder UseUamTenantValidation(this IApplicationBuilder app)
    {
        return app.Use(async (context, next) =>
        {
            var requiresTenant = context.Request.Path.StartsWithSegments(TenancyConstants.ApiPrefix, StringComparison.OrdinalIgnoreCase)
                                 && !string.Equals(context.Request.Path.Value, TenancyConstants.HealthPath, StringComparison.OrdinalIgnoreCase)
                                 && !HttpMethods.IsOptions(context.Request.Method);
            requiresTenant = requiresTenant || IsGrpcRequest(context);

            if (!requiresTenant)
            {
                await next();
                return;
            }

            var tenantClaim = context.User.FindFirstValue(TenancyConstants.TenantClaimName)?.Trim();
            var tenantHeader = context.Request.Headers[TenancyConstants.TenantHeaderName].ToString().Trim();
            var hasTenantClaim = !string.IsNullOrWhiteSpace(tenantClaim);
            var hasTenantHeader = !string.IsNullOrWhiteSpace(tenantHeader);

            if (hasTenantClaim && !TenantIdPattern.IsMatch(tenantClaim!))
            {
                await Results.Problem(
                        statusCode: StatusCodes.Status401Unauthorized,
                        title: "Invalid token",
                        detail: $"Claim '{TenancyConstants.TenantClaimName}' must be a valid tenant identifier.")
                    .ExecuteAsync(context);
                return;
            }

            if (hasTenantHeader && !TenantIdPattern.IsMatch(tenantHeader))
            {
                await Results.Problem(
                        statusCode: StatusCodes.Status400BadRequest,
                        title: "Invalid tenant header",
                        detail: $"Header '{TenancyConstants.TenantHeaderName}' must be a valid tenant identifier.")
                    .ExecuteAsync(context);
                return;
            }

            if (hasTenantClaim && hasTenantHeader && !string.Equals(tenantClaim, tenantHeader, StringComparison.Ordinal))
            {
                await Results.Problem(
                        statusCode: StatusCodes.Status403Forbidden,
                        title: "Tenant mismatch",
                        detail: $"Claim '{TenancyConstants.TenantClaimName}' and header '{TenancyConstants.TenantHeaderName}' must match when both are provided.")
                    .ExecuteAsync(context);
                return;
            }

            if (!hasTenantClaim && !hasTenantHeader)
            {
                await Results.Problem(
                        statusCode: StatusCodes.Status400BadRequest,
                        title: "Missing tenant header",
                        detail: $"Requests under {TenancyConstants.ApiPrefix} require an {TenancyConstants.TenantHeaderName} header when the token does not include '{TenancyConstants.TenantClaimName}'.")
                    .ExecuteAsync(context);
                return;
            }

            var tenantId = hasTenantClaim ? tenantClaim! : tenantHeader;

            if (!(context.User.Identity?.IsAuthenticated ?? false))
            {
                await Results.Problem(
                        statusCode: StatusCodes.Status401Unauthorized,
                        title: "Authentication required",
                        detail: "A valid bearer token is required to access this endpoint.")
                    .ExecuteAsync(context);
                return;
            }

            var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier)
                         ?? context.User.FindFirstValue("sub");
            if (string.IsNullOrWhiteSpace(userId))
            {
                await Results.Problem(
                        statusCode: StatusCodes.Status401Unauthorized,
                        title: "Invalid token",
                        detail: "The access token does not contain a subject identifier.")
                    .ExecuteAsync(context);
                return;
            }

            var tenantDirectoryClient = context.RequestServices.GetRequiredService<ITenantDirectoryClient>();
            var identityClient = context.RequestServices.GetRequiredService<IIdentityAccessClient>();
            var tenantContextAccessor = context.RequestServices.GetRequiredService<ITenantContextAccessor>();

            TenantLookupResult tenant;
            TenantAccessResult access;

            try
            {
                tenant = await tenantDirectoryClient.GetTenantAsync(tenantId, context.RequestAborted);
                access = await identityClient.AuthorizeTenantAccessAsync(tenantId, userId, context.RequestAborted);
            }
            catch (RemoteDependencyException ex)
            {
                await Results.Problem(
                        statusCode: StatusCodes.Status503ServiceUnavailable,
                        title: "Dependency unavailable",
                        detail: ex.Message)
                    .ExecuteAsync(context);
                return;
            }

            if (!tenant.Exists)
            {
                await Results.Problem(
                        statusCode: StatusCodes.Status404NotFound,
                        title: "Tenant not found",
                        detail: $"Tenant '{tenantId}' does not exist.")
                    .ExecuteAsync(context);
                return;
            }

            if (!tenant.IsActive)
            {
                await Results.Problem(
                        statusCode: StatusCodes.Status403Forbidden,
                        title: "Tenant inactive",
                        detail: $"Tenant '{tenantId}' is inactive.")
                    .ExecuteAsync(context);
                return;
            }

            if (!access.IsAllowed)
            {
                await Results.Problem(
                        statusCode: StatusCodes.Status403Forbidden,
                        title: "Tenant access denied",
                        detail: $"The authenticated user does not have access to tenant '{tenantId}'.")
                    .ExecuteAsync(context);
                return;
            }

            tenantContextAccessor.SetCurrent(new TenantContext(tenantId, userId, access.Roles, access.Permissions));
            await next();
        });
    }

    private static bool IsGrpcRequest(HttpContext context)
    {
        var contentType = context.Request.ContentType;
        return !string.IsNullOrWhiteSpace(contentType)
               && contentType.StartsWith("application/grpc", StringComparison.OrdinalIgnoreCase);
    }
}
