using Microsoft.EntityFrameworkCore;
using UAM.Context;
using UAM.Dtos.Common;
using UAM.Dtos.Stories;
using UAM.Repositories;
using UAM.Services.Common;
using ContentModel = UAM.Models.Content;

namespace UAM.Services.Stories;

public interface IStoryService
{
    Task<PagedResponse<StoryResponse>> ListAsync(StoryListQuery query, CancellationToken cancellationToken);
    Task<StoryResponse?> GetAsync(string id, bool includeDeleted, CancellationToken cancellationToken);
    Task<StoryResponse> CreateAsync(StoryUpsertRequest request, CancellationToken cancellationToken);
    Task<StoryResponse?> UpdateAsync(string id, StoryUpsertRequest request, CancellationToken cancellationToken);
    Task<bool> DeleteAsync(string id, CancellationToken cancellationToken);
    Task<StoryResponse?> RestoreAsync(string id, CancellationToken cancellationToken);
    Task<StoryResponse?> PublishAsync(string id, bool publish, CancellationToken cancellationToken);
    Task<StoryResponse?> IncrementViewsAsync(string id, int delta, CancellationToken cancellationToken);
    Task<StoryResponse?> IncrementLikesAsync(string id, int delta, CancellationToken cancellationToken);
    Task<StoryResponse?> IncrementDislikesAsync(string id, int delta, CancellationToken cancellationToken);
}

public sealed class StoryService(AppDbContext context, IRepository<ContentModel> repository) : IStoryService
{
    private const string SystemUser = "system";

    public async Task<PagedResponse<StoryResponse>> ListAsync(StoryListQuery query, CancellationToken cancellationToken)
    {
        var stories = BuildStoryQuery(query);
        return await ServicePaging.ToPagedResponseAsync(stories, query.Pagination, cancellationToken);
    }

    public async Task<StoryResponse?> GetAsync(string id, bool includeDeleted, CancellationToken cancellationToken)
    {
        return await BuildStoryQuery(new StoryListQuery(IncludeDeleted: includeDeleted), id)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<StoryResponse> CreateAsync(StoryUpsertRequest request, CancellationToken cancellationToken)
    {
        var normalizedTitle = NormalizeRequired(request.Title, nameof(request.Title));
        var normalizedSlug = NormalizeRequired(request.Slug, nameof(request.Slug));

        await EnsureUniqueSlugAsync(normalizedSlug, null, cancellationToken);

        var story = new ContentModel
        {
            Title = normalizedTitle,
            Slug = normalizedSlug,
            Description = request.Description?.Trim(),
            Value = request.Value,
            IsPublished = request.IsPublished,
            PublishedAt = request.IsPublished ? request.PublishedAt ?? DateTimeOffset.UtcNow : null,
            ViewCount = request.ViewCount,
            LikeCount = request.LikeCount,
            DislikeCount = request.DislikeCount,
            CreatedBy = SystemUser,
            UpdatedBy = SystemUser
        };

        await repository.AddAsync(story, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);

        return (await GetAsync(story.Id, false, cancellationToken))!;
    }

    public async Task<StoryResponse?> UpdateAsync(string id, StoryUpsertRequest request, CancellationToken cancellationToken)
    {
        var story = await repository.FindAsync(id, cancellationToken);
        if (story is null) return null;

        var normalizedTitle = NormalizeRequired(request.Title, nameof(request.Title));
        var normalizedSlug = NormalizeRequired(request.Slug, nameof(request.Slug));

        await EnsureUniqueSlugAsync(normalizedSlug, id, cancellationToken);

        story.Title = normalizedTitle;
        story.Slug = normalizedSlug;
        story.Description = request.Description?.Trim();
        story.Value = request.Value;
        story.IsPublished = request.IsPublished;
        story.PublishedAt = request.IsPublished ? request.PublishedAt ?? story.PublishedAt ?? DateTimeOffset.UtcNow : null;
        story.ViewCount = request.ViewCount;
        story.LikeCount = request.LikeCount;
        story.DislikeCount = request.DislikeCount;
        story.UpdatedBy = SystemUser;

        await context.SaveChangesAsync(cancellationToken);
        return await GetAsync(id, true, cancellationToken);
    }

    public async Task<bool> DeleteAsync(string id, CancellationToken cancellationToken)
    {
        var story = await repository.FindAsync(id, cancellationToken);
        if (story is null) return false;

        story.MarkDeleted(SystemUser, DateTimeOffset.UtcNow);
        story.UpdatedBy = SystemUser;
        await context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<StoryResponse?> RestoreAsync(string id, CancellationToken cancellationToken)
    {
        var story = await repository.FindAsync(id, cancellationToken, includeDeleted: true);
        if (story is null) return null;

        story.Restore();
        story.UpdatedBy = SystemUser;
        await context.SaveChangesAsync(cancellationToken);
        return await GetAsync(id, false, cancellationToken);
    }

    public async Task<StoryResponse?> PublishAsync(string id, bool publish, CancellationToken cancellationToken)
    {
        var story = await repository.FindAsync(id, cancellationToken);
        if (story is null) return null;

        story.IsPublished = publish;
        story.PublishedAt = publish ? story.PublishedAt ?? DateTimeOffset.UtcNow : null;
        story.UpdatedBy = SystemUser;
        await context.SaveChangesAsync(cancellationToken);
        return await GetAsync(id, false, cancellationToken);
    }

    public async Task<StoryResponse?> IncrementViewsAsync(string id, int delta, CancellationToken cancellationToken)
    {
        return await UpdateCounterAsync(id, delta, story => story.ViewCount, (story, value) => story.ViewCount = value, cancellationToken);
    }

    public async Task<StoryResponse?> IncrementLikesAsync(string id, int delta, CancellationToken cancellationToken)
    {
        return await UpdateCounterAsync(id, delta, story => story.LikeCount, (story, value) => story.LikeCount = value, cancellationToken);
    }

    public async Task<StoryResponse?> IncrementDislikesAsync(string id, int delta, CancellationToken cancellationToken)
    {
        return await UpdateCounterAsync(id, delta, story => story.DislikeCount, (story, value) => story.DislikeCount = value, cancellationToken);
    }

    private IQueryable<StoryResponse> BuildStoryQuery(StoryListQuery query, string? id = null)
    {
        var stories = repository.Query(query.IncludeDeleted).AsNoTracking();
        stories = ServiceGuards.ApplySearch(stories, query.Search, entity => entity.Title, entity => entity.Slug, entity => entity.Description, entity => entity.Value);

        if (!string.IsNullOrWhiteSpace(id)) stories = stories.Where(entity => entity.Id == id);
        if (query.IsPublished is not null) stories = stories.Where(entity => entity.IsPublished == query.IsPublished.Value);

        return stories
            .OrderByDescending(entity => entity.UpdatedAt)
            .Select(entity => new StoryResponse(
                entity.Id,
                entity.TenantId,
                entity.Title,
                entity.Slug,
                entity.Description,
                entity.Value,
                entity.IsPublished,
                entity.PublishedAt,
                entity.ViewCount,
                entity.LikeCount,
                entity.DislikeCount,
                entity.IsDeleted,
                entity.DeletedAt,
                entity.DeletedBy,
                entity.CreatedAt,
                entity.UpdatedAt,
                entity.CreatedBy,
                entity.UpdatedBy));
    }

    private async Task EnsureUniqueSlugAsync(string slug, string? storyId, CancellationToken cancellationToken)
    {
        await ServiceGuards.EnsureDoesNotExistAsync(
            context.Story,
            entity => entity.Slug == slug && entity.Id != storyId,
            $"A story with slug '{slug}' already exists.",
            cancellationToken);
    }

    private async Task<StoryResponse?> UpdateCounterAsync(
        string id,
        int delta,
        Func<ContentModel, int> getCounter,
        Action<ContentModel, int> setCounter,
        CancellationToken cancellationToken)
    {
        if (delta <= 0) throw new InvalidOperationException("Counter delta must be greater than zero.");

        var story = await repository.FindAsync(id, cancellationToken);
        if (story is null) return null;

        int updatedValue;
        try
        {
            updatedValue = checked(getCounter(story) + delta);
        }
        catch (OverflowException)
        {
            throw new InvalidOperationException("Counter update exceeds the supported range.");
        }

        setCounter(story, updatedValue);
        story.UpdatedBy = SystemUser;
        await context.SaveChangesAsync(cancellationToken);
        return await GetAsync(id, false, cancellationToken);
    }

    private static string NormalizeRequired(string? value, string fieldName)
    {
        var normalized = value?.Trim();
        if (string.IsNullOrWhiteSpace(normalized))
            throw new InvalidOperationException($"{fieldName} is required.");

        return normalized;
    }
}
