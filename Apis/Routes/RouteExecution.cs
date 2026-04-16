using Microsoft.AspNetCore.Http.HttpResults;
using UAM.Dtos.Common;

namespace UAM.Apis.Routes;

internal static class RouteExecution
{
    public static async Task<Results<Ok<ApiResponse<PagedResponse<T>>>, BadRequest<ApiResponse<PagedResponse<T>>>>> QueryPage<T>(
        Func<Task<PagedResponse<T>>> action)
    {
        try
        {
            return RouteResults.Paged(await action());
        }
        catch (InvalidOperationException ex)
        {
            return RouteResults.BadRequest<PagedResponse<T>>(ex.Message);
        }
    }

    public static async Task<Results<Ok<ApiResponse<T>>, BadRequest<ApiResponse<T>>>> QueryOne<T>(Func<Task<T>> action)
    {
        try
        {
            return RouteResults.Ok(await action());
        }
        catch (InvalidOperationException ex)
        {
            return RouteResults.BadRequest<T>(ex.Message);
        }
    }

    public static async Task<Results<Ok<ApiResponse<T>>, NotFound, BadRequest<ApiResponse<T>>>> QueryMaybe<T>(Func<Task<T?>> action)
    {
        try
        {
            var item = await action();
            return item is null ? TypedResults.NotFound() : RouteResults.Ok(item);
        }
        catch (InvalidOperationException ex)
        {
            return RouteResults.BadRequest<T>(ex.Message);
        }
    }

    public static async Task<Results<Ok<ApiResponse<T>>, NotFound, BadRequest<ApiResponse<T>>>> QueryMaybe<T>(Func<Task<T?>> action, string successMessage)
    {
        try
        {
            var item = await action();
            return item is null ? TypedResults.NotFound() : RouteResults.Ok(item, successMessage);
        }
        catch (InvalidOperationException ex)
        {
            return RouteResults.BadRequest<T>(ex.Message);
        }
    }
}

