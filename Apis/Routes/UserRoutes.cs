using Microsoft.AspNetCore.Http.HttpResults;
using System.Security.Claims;
using UAM.Dtos.Common;
using UAM.Dtos.Users;
using UAM.Services.Users;

namespace UAM.Apis.Routes;

public static class UserRoutes
{
    public static RouteGroupBuilder RegisterUserRoutes(this RouteGroupBuilder route)
    {
        route.MapPost("/", Create).WithName("Users_Create").WithSummary("Create user profile");
        route.MapGet("/{userId}", Get).WithName("Users_Get").WithSummary("Get user");
        route.MapGet("/", List).WithName("Users_List").WithSummary("List/search users");
        route.MapPut("/{userId}", Update).WithName("Users_Update").WithSummary("Update user profile");
        route.MapPatch("/{userId}", Patch).WithName("Users_Patch").WithSummary("Patch user profile");
        route.MapDelete("/{userId}", Delete).WithName("Users_Delete").WithSummary("Soft delete user");
        route.MapPost("/{userId}/restore", Restore).WithName("Users_Restore").WithSummary("Restore user");
        route.MapPost("/{userId}/activate", Activate).WithName("Users_Activate").WithSummary("Activate user");
        route.MapPost("/{userId}/deactivate", Deactivate).WithName("Users_Deactivate").WithSummary("Deactivate user");
        route.MapGet("/{userId}/preferences", GetPreferences).WithName("Users_GetPreferences").WithSummary("Get user preferences");
        route.MapPut("/{userId}/preferences", PutPreferences).WithName("Users_PutPreferences").WithSummary("Update user preferences");
        return route;
    }

    public static RouteGroupBuilder RegisterMeRoutes(this RouteGroupBuilder route)
    {
        route.MapGet("/me", GetMe).WithName("Users_GetMe").WithSummary("Get current user profile");
        route.MapPut("/me", PutMe).WithName("Users_PutMe").WithSummary("Update current user profile");
        route.MapPatch("/me", PatchMe).WithName("Users_PatchMe").WithSummary("Patch current user profile");
        return route;
    }

    private static async Task<Results<Ok<ApiResponse<PagedResponse<UserResponse>>>, BadRequest<ApiResponse<PagedResponse<UserResponse>>>>> List(
        [AsParameters] UserListQuery query,
        IUserService service,
        CancellationToken cancellationToken)
    {
        return await RouteExecution.QueryPage(() => service.ListAsync(query, cancellationToken));
    }

    private static async Task<Results<Ok<ApiResponse<UserResponse>>, NotFound, BadRequest<ApiResponse<UserResponse>>>> Get(
        string userId,
        IUserService service,
        CancellationToken cancellationToken)
    {
        return await RouteExecution.QueryMaybe(() => service.GetAsync(userId, false, cancellationToken));
    }

    private static async Task<Results<Ok<ApiResponse<UserResponse>>, BadRequest<ApiResponse<UserResponse>>>> Create(
        UserCreateRequest request,
        IUserService service,
        CancellationToken cancellationToken)
    {
        return await RouteExecution.QueryOne(() => service.CreateAsync(request, cancellationToken));
    }

    private static async Task<Results<Ok<ApiResponse<UserResponse>>, NotFound, BadRequest<ApiResponse<UserResponse>>>> Update(
        string userId,
        UserUpdateRequest request,
        IUserService service,
        CancellationToken cancellationToken)
    {
        return await RouteExecution.QueryMaybe(() => service.UpdateAsync(userId, request, cancellationToken));
    }

    private static async Task<Results<Ok<ApiResponse<UserResponse>>, NotFound, BadRequest<ApiResponse<UserResponse>>>> Patch(
        string userId,
        UserPatchRequest request,
        IUserService service,
        CancellationToken cancellationToken)
    {
        return await RouteExecution.QueryMaybe(() => service.PatchAsync(userId, request, cancellationToken));
    }

    private static async Task<Results<Ok<ApiResponse<bool>>, NotFound>> Delete(
        string userId,
        IUserService service,
        CancellationToken cancellationToken)
    {
        return await service.DeleteAsync(userId, cancellationToken) ? RouteResults.Ok(true, "User deleted") : TypedResults.NotFound();
    }

    private static async Task<Results<Ok<ApiResponse<UserResponse>>, NotFound>> Restore(
        string userId,
        IUserService service,
        CancellationToken cancellationToken)
    {
        var user = await service.RestoreAsync(userId, cancellationToken);
        return user is null ? TypedResults.NotFound() : RouteResults.Ok(user, "User restored");
    }

    private static async Task<Results<Ok<ApiResponse<UserResponse>>, NotFound>> Activate(
        string userId,
        IUserService service,
        CancellationToken cancellationToken)
    {
        var user = await service.SetActiveAsync(userId, true, cancellationToken);
        return user is null ? TypedResults.NotFound() : RouteResults.Ok(user, "User activated");
    }

    private static async Task<Results<Ok<ApiResponse<UserResponse>>, NotFound>> Deactivate(
        string userId,
        IUserService service,
        CancellationToken cancellationToken)
    {
        var user = await service.SetActiveAsync(userId, false, cancellationToken);
        return user is null ? TypedResults.NotFound() : RouteResults.Ok(user, "User deactivated");
    }

    private static async Task<Results<Ok<ApiResponse<UserPreferencesResponse>>, NotFound>> GetPreferences(
        string userId,
        IUserService service,
        CancellationToken cancellationToken)
    {
        var preferences = await service.GetPreferencesAsync(userId, cancellationToken);
        return preferences is null ? TypedResults.NotFound() : RouteResults.Ok(preferences);
    }

    private static async Task<Results<Ok<ApiResponse<UserPreferencesResponse>>, NotFound, BadRequest<ApiResponse<UserPreferencesResponse>>>> PutPreferences(
        string userId,
        UserPreferencesRequest request,
        IUserService service,
        CancellationToken cancellationToken)
    {
        return await RouteExecution.QueryMaybe(() => service.UpdatePreferencesAsync(userId, request, cancellationToken), "User preferences updated");
    }

    private static async Task<Results<Ok<ApiResponse<UserResponse>>, NotFound, BadRequest<ApiResponse<UserResponse>>>> GetMe(
        ClaimsPrincipal principal,
        IUserService service,
        CancellationToken cancellationToken)
    {
        return await RouteExecution.QueryMaybe(() => service.GetMeAsync(UserActorResolver.Resolve(principal), cancellationToken));
    }

    private static async Task<Results<Ok<ApiResponse<UserResponse>>, NotFound, BadRequest<ApiResponse<UserResponse>>>> PutMe(
        ClaimsPrincipal principal,
        UserMeUpdateRequest request,
        IUserService service,
        CancellationToken cancellationToken)
    {
        return await RouteExecution.QueryMaybe(() => service.UpdateMeAsync(UserActorResolver.Resolve(principal), request, cancellationToken), "Profile updated");
    }

    private static async Task<Results<Ok<ApiResponse<UserResponse>>, NotFound, BadRequest<ApiResponse<UserResponse>>>> PatchMe(
        ClaimsPrincipal principal,
        UserMeUpdatePatchRequest request,
        IUserService service,
        CancellationToken cancellationToken)
    {
        var actor = UserActorResolver.Resolve(principal);
        var (result, validationError) = await service.PatchMeAsync(actor, request, cancellationToken);
        if (validationError is not null)
            return TypedResults.BadRequest(ApiResponse<UserResponse>.Fail(validationError));
        return result is null
            ? TypedResults.NotFound()
            : RouteResults.Ok(result, "Profile updated");
    }
}
