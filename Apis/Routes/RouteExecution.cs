using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Grpc.Core;
using Slogtry.Grpc;
using UAM.Dtos.Common;

namespace UAM.Apis.Routes;

internal static class RouteExecution
{
    public static async Task<Results<Ok<ApiResponse<PagedResponse<T>>>, BadRequest<ApiResponse<PagedResponse<T>>>>> QueryPage<T>(
        Func<Task<PagedResponse<T>>> action,
        ILogger? logger = null)
    {
        try
        {
            return RouteResults.Paged(await action());
        }
        catch (InvalidOperationException ex)
        {
            return RouteResults.BadRequest<PagedResponse<T>>(ex.Message);
        }
        catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
        {
            return RouteResults.BadRequest<PagedResponse<T>>("A record with the same unique value already exists.");
        }
        catch (DbUpdateException ex)
        {
            logger?.LogError(ex, "Database update failed");
            return RouteResults.BadRequest<PagedResponse<T>>("A database error occurred while processing the request.");
        }
        catch (RpcException ex)
        {
            logger?.LogError(ex, "gRPC dependency call failed with status {StatusCode}", ex.StatusCode);
            return RouteResults.BadRequest<PagedResponse<T>>("A dependent service is temporarily unavailable. Please try again later.");
        }
        catch (RemoteDependencyException ex)
        {
            logger?.LogError(ex, "Remote dependency call failed");
            return RouteResults.BadRequest<PagedResponse<T>>("A dependent service is temporarily unavailable. Please try again later.");
        }
        catch (OperationCanceledException)
        {
            logger?.LogInformation("Request was cancelled by the client");
            return RouteResults.BadRequest<PagedResponse<T>>("Request was cancelled.");
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "An unexpected error occurred");
            return RouteResults.BadRequest<PagedResponse<T>>("An unexpected error occurred.");
        }
    }

    public static async Task<Results<Ok<ApiResponse<T>>, BadRequest<ApiResponse<T>>>> QueryOne<T>(
        Func<Task<T>> action,
        ILogger? logger = null)
    {
        try
        {
            return RouteResults.Ok(await action());
        }
        catch (InvalidOperationException ex)
        {
            return RouteResults.BadRequest<T>(ex.Message);
        }
        catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
        {
            return RouteResults.BadRequest<T>("A record with the same unique value already exists.");
        }
        catch (DbUpdateException ex)
        {
            logger?.LogError(ex, "Database update failed");
            return RouteResults.BadRequest<T>("A database error occurred while processing the request.");
        }
        catch (RpcException ex)
        {
            logger?.LogError(ex, "gRPC dependency call failed with status {StatusCode}", ex.StatusCode);
            return RouteResults.BadRequest<T>("A dependent service is temporarily unavailable. Please try again later.");
        }
        catch (RemoteDependencyException ex)
        {
            logger?.LogError(ex, "Remote dependency call failed");
            return RouteResults.BadRequest<T>("A dependent service is temporarily unavailable. Please try again later.");
        }
        catch (OperationCanceledException)
        {
            logger?.LogInformation("Request was cancelled by the client");
            return RouteResults.BadRequest<T>("Request was cancelled.");
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "An unexpected error occurred");
            return RouteResults.BadRequest<T>("An unexpected error occurred.");
        }
    }

    public static async Task<Results<Ok<ApiResponse<T>>, NotFound, BadRequest<ApiResponse<T>>>> QueryMaybe<T>(
        Func<Task<T?>> action,
        ILogger? logger = null)
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
        catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
        {
            return RouteResults.BadRequest<T>("A record with the same unique value already exists.");
        }
        catch (DbUpdateException ex)
        {
            logger?.LogError(ex, "Database update failed");
            return RouteResults.BadRequest<T>("A database error occurred while processing the request.");
        }
        catch (RpcException ex)
        {
            logger?.LogError(ex, "gRPC dependency call failed with status {StatusCode}", ex.StatusCode);
            return RouteResults.BadRequest<T>("A dependent service is temporarily unavailable. Please try again later.");
        }
        catch (RemoteDependencyException ex)
        {
            logger?.LogError(ex, "Remote dependency call failed");
            return RouteResults.BadRequest<T>("A dependent service is temporarily unavailable. Please try again later.");
        }
        catch (OperationCanceledException)
        {
            logger?.LogInformation("Request was cancelled by the client");
            return RouteResults.BadRequest<T>("Request was cancelled.");
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "An unexpected error occurred");
            return RouteResults.BadRequest<T>("An unexpected error occurred.");
        }
    }

    public static async Task<Results<Ok<ApiResponse<T>>, NotFound, BadRequest<ApiResponse<T>>>> QueryMaybe<T>(
        Func<Task<T?>> action,
        string successMessage,
        ILogger? logger = null)
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
        catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
        {
            return RouteResults.BadRequest<T>("A record with the same unique value already exists.");
        }
        catch (DbUpdateException ex)
        {
            logger?.LogError(ex, "Database update failed");
            return RouteResults.BadRequest<T>("A database error occurred while processing the request.");
        }
        catch (RpcException ex)
        {
            logger?.LogError(ex, "gRPC dependency call failed with status {StatusCode}", ex.StatusCode);
            return RouteResults.BadRequest<T>("A dependent service is temporarily unavailable. Please try again later.");
        }
        catch (RemoteDependencyException ex)
        {
            logger?.LogError(ex, "Remote dependency call failed");
            return RouteResults.BadRequest<T>("A dependent service is temporarily unavailable. Please try again later.");
        }
        catch (OperationCanceledException)
        {
            logger?.LogInformation("Request was cancelled by the client");
            return RouteResults.BadRequest<T>("Request was cancelled.");
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "An unexpected error occurred");
            return RouteResults.BadRequest<T>("An unexpected error occurred.");
        }
    }

    private static bool IsUniqueConstraintViolation(DbUpdateException ex)
    {
        var message = ex.InnerException?.Message ?? ex.Message;
        return message.Contains("unique constraint", StringComparison.OrdinalIgnoreCase)
               || message.Contains("duplicate key", StringComparison.OrdinalIgnoreCase)
               || message.Contains("UNIQUE constraint failed", StringComparison.OrdinalIgnoreCase)
               || message.Contains("23505", StringComparison.Ordinal);
    }
}
