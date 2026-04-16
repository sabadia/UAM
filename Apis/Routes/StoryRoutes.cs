using Microsoft.AspNetCore.Http.HttpResults;
using UAM.Dtos.Common;
using UAM.Dtos.Stories;
using UAM.Services.Stories;

namespace UAM.Apis.Routes;

public static class StoryRoutes
{
    public static RouteGroupBuilder RegisterStoryRoutes(this RouteGroupBuilder route)
    {
        route.MapGet("/", List).WithName("Stories_List").WithSummary("List stories");
        route.MapGet("/{id}", Get).WithName("Stories_Get").WithSummary("Get story");
        route.MapPost("/", Create).WithName("Stories_Create").WithSummary("Create story");
        route.MapPut("/{id}", Update).WithName("Stories_Update").WithSummary("Update story");
        route.MapDelete("/{id}", Delete).WithName("Stories_Delete").WithSummary("Soft delete story");
        route.MapPost("/{id}/restore", Restore).WithName("Stories_Restore").WithSummary("Restore story");
        route.MapPost("/{id}/publish", Publish).WithName("Stories_Publish").WithSummary("Publish story");
        route.MapDelete("/{id}/publish", Unpublish).WithName("Stories_Unpublish").WithSummary("Unpublish story");
        route.MapPost("/{id}/views", AddView).WithName("Stories_AddView").WithSummary("Increase story views");
        route.MapPost("/{id}/likes", AddLike).WithName("Stories_AddLike").WithSummary("Increase story likes");
        route.MapPost("/{id}/dislikes", AddDislike).WithName("Stories_AddDislike").WithSummary("Increase story dislikes");
        return route;
    }

    private static async Task<Results<Ok<ApiResponse<PagedResponse<StoryResponse>>>, BadRequest<ApiResponse<PagedResponse<StoryResponse>>>>> List(
        [AsParameters] StoryListQuery query,
        IStoryService service,
        CancellationToken cancellationToken)
    {
        return await RouteExecution.QueryPage(() => service.ListAsync(query, cancellationToken));
    }

    private static async Task<Results<Ok<ApiResponse<StoryResponse>>, NotFound, BadRequest<ApiResponse<StoryResponse>>>> Get(
        string id,
        IStoryService service,
        CancellationToken cancellationToken)
    {
        return await RouteExecution.QueryMaybe(() => service.GetAsync(id, false, cancellationToken));
    }

    private static async Task<Results<Ok<ApiResponse<StoryResponse>>, BadRequest<ApiResponse<StoryResponse>>>> Create(
        StoryUpsertRequest request,
        IStoryService service,
        CancellationToken cancellationToken)
    {
        return await RouteExecution.QueryOne(() => service.CreateAsync(request, cancellationToken));
    }

    private static async Task<Results<Ok<ApiResponse<StoryResponse>>, NotFound, BadRequest<ApiResponse<StoryResponse>>>> Update(
        string id,
        StoryUpsertRequest request,
        IStoryService service,
        CancellationToken cancellationToken)
    {
        return await RouteExecution.QueryMaybe(() => service.UpdateAsync(id, request, cancellationToken));
    }

    private static async Task<Results<Ok<ApiResponse<bool>>, NotFound>> Delete(
        string id,
        IStoryService service,
        CancellationToken cancellationToken)
    {
        return await service.DeleteAsync(id, cancellationToken) ? RouteResults.Ok(true, "Story deleted") : TypedResults.NotFound();
    }

    private static async Task<Results<Ok<ApiResponse<StoryResponse>>, NotFound>> Restore(
        string id,
        IStoryService service,
        CancellationToken cancellationToken)
    {
        var story = await service.RestoreAsync(id, cancellationToken);
        return story is null ? TypedResults.NotFound() : RouteResults.Ok(story, "Story restored");
    }

    private static async Task<Results<Ok<ApiResponse<StoryResponse>>, NotFound>> Publish(
        string id,
        IStoryService service,
        CancellationToken cancellationToken)
    {
        var story = await service.PublishAsync(id, true, cancellationToken);
        return story is null ? TypedResults.NotFound() : RouteResults.Ok(story, "Story published");
    }

    private static async Task<Results<Ok<ApiResponse<StoryResponse>>, NotFound>> Unpublish(
        string id,
        IStoryService service,
        CancellationToken cancellationToken)
    {
        var story = await service.PublishAsync(id, false, cancellationToken);
        return story is null ? TypedResults.NotFound() : RouteResults.Ok(story, "Story unpublished");
    }

    private static async Task<Results<Ok<ApiResponse<StoryResponse>>, NotFound, BadRequest<ApiResponse<StoryResponse>>>> AddView(
        string id,
        StoryCounterRequest request,
        IStoryService service,
        CancellationToken cancellationToken)
    {
        return await RouteExecution.QueryMaybe(() => service.IncrementViewsAsync(id, request.Delta, cancellationToken), "View count updated");
    }

    private static async Task<Results<Ok<ApiResponse<StoryResponse>>, NotFound, BadRequest<ApiResponse<StoryResponse>>>> AddLike(
        string id,
        StoryCounterRequest request,
        IStoryService service,
        CancellationToken cancellationToken)
    {
        return await RouteExecution.QueryMaybe(() => service.IncrementLikesAsync(id, request.Delta, cancellationToken), "Like count updated");
    }

    private static async Task<Results<Ok<ApiResponse<StoryResponse>>, NotFound, BadRequest<ApiResponse<StoryResponse>>>> AddDislike(
        string id,
        StoryCounterRequest request,
        IStoryService service,
        CancellationToken cancellationToken)
    {
        return await RouteExecution.QueryMaybe(() => service.IncrementDislikesAsync(id, request.Delta, cancellationToken), "Dislike count updated");
    }
}
