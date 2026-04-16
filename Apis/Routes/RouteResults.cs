using Microsoft.AspNetCore.Http.HttpResults;
using UAM.Dtos.Common;

namespace UAM.Apis.Routes;

internal static class RouteResults
{
    public static Ok<ApiResponse<T>> Ok<T>(T data, string? message = null)
        => TypedResults.Ok(ApiResponse<T>.Ok(data, message));

    public static Ok<ApiResponse<PagedResponse<T>>> Paged<T>(PagedResponse<T> data, string? message = null)
        => TypedResults.Ok(ApiResponse<PagedResponse<T>>.Ok(data, message));

    public static BadRequest<ApiResponse<T>> BadRequest<T>(string message, params string[] errors)
        => TypedResults.BadRequest(ApiResponse<T>.Fail(message, errors));
}

