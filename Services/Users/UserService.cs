using System.Security.Claims;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using UAM.Context;
using UAM.Dtos.Common;
using UAM.Dtos.Users;
using UAM.Models;
using UAM.Services.Common;

namespace UAM.Services.Users;

public sealed record UserActor(string? InternalUserId, string? ExternalAuthUserId);

public static class UserActorResolver
{
    public const string ExplicitUserIdClaim = "user_id";

    public static UserActor Resolve(ClaimsPrincipal principal)
    {
        var internalUserId = principal.FindFirstValue(ExplicitUserIdClaim)?.Trim();
        var externalAuthUserId = principal.FindFirstValue("sub")?.Trim()
                                 ?? principal.FindFirstValue(ClaimTypes.NameIdentifier)?.Trim();

        if (string.IsNullOrWhiteSpace(internalUserId) && string.IsNullOrWhiteSpace(externalAuthUserId))
            throw new InvalidOperationException("The token does not contain a usable user identifier.");

        return new UserActor(
            string.IsNullOrWhiteSpace(internalUserId) ? null : internalUserId,
            string.IsNullOrWhiteSpace(externalAuthUserId) ? null : externalAuthUserId);
    }
}

public interface IUserService
{
    Task<PagedResponse<UserResponse>> ListAsync(UserListQuery query, CancellationToken cancellationToken);
    Task<UserResponse?> GetAsync(string id, bool includeDeleted, CancellationToken cancellationToken);
    Task<UserResponse> CreateAsync(UserCreateRequest request, CancellationToken cancellationToken);
    Task<UserResponse?> UpdateAsync(string id, UserUpdateRequest request, CancellationToken cancellationToken);
    Task<UserResponse?> PatchAsync(string id, UserPatchRequest request, CancellationToken cancellationToken);
    Task<bool> DeleteAsync(string id, CancellationToken cancellationToken);
    Task<UserResponse?> RestoreAsync(string id, CancellationToken cancellationToken);
    Task<UserResponse?> SetActiveAsync(string id, bool active, CancellationToken cancellationToken);
    Task<UserPreferencesResponse?> GetPreferencesAsync(string id, CancellationToken cancellationToken);
    Task<UserPreferencesResponse?> UpdatePreferencesAsync(string id, UserPreferencesRequest request, CancellationToken cancellationToken);
    Task<UserResponse?> GetMeAsync(UserActor actor, CancellationToken cancellationToken);
    Task<UserResponse?> UpdateMeAsync(UserActor actor, UserMeUpdateRequest request, CancellationToken cancellationToken);
    Task<IReadOnlyList<UserResponse>> BatchGetAsync(IReadOnlyCollection<string> ids, bool includeDeleted, CancellationToken cancellationToken);
    Task<UserProfileSummaryResponse?> GetProfileSummaryAsync(string id, CancellationToken cancellationToken);
}

public sealed class UserService(
    AppDbContext context,
    IRepository<UserProfile> repository,
    ITenantContextAccessor tenantContextAccessor) : IUserService
{
    private const string SystemUser = "system";

    public async Task<PagedResponse<UserResponse>> ListAsync(UserListQuery query, CancellationToken cancellationToken)
    {
        var users = BuildUserQuery(query);
        return await ServicePaging.ToPagedResponseAsync(users, query.Pagination, cancellationToken);
    }

    public async Task<UserResponse?> GetAsync(string id, bool includeDeleted, CancellationToken cancellationToken)
    {
        return await BuildUserQuery(new UserListQuery(IncludeDeleted: includeDeleted), id)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<UserResponse> CreateAsync(UserCreateRequest request, CancellationToken cancellationToken)
    {
        var externalAuthUserId = NormalizeRequired(request.ExternalAuthUserId, nameof(request.ExternalAuthUserId));
        var email = NormalizeEmail(request.Email);
        var displayName = NormalizeRequired(request.DisplayName, nameof(request.DisplayName));

        await EnsureUniqueExternalAuthUserIdAsync(externalAuthUserId, null, cancellationToken);
        await EnsureUniqueEmailAsync(email, null, cancellationToken);

        var user = new UserProfile
        {
            ExternalAuthUserId = externalAuthUserId,
            Email = email,
            DisplayName = displayName,
            FirstName = NormalizeOptional(request.FirstName),
            LastName = NormalizeOptional(request.LastName),
            PhoneNumber = NormalizeOptional(request.PhoneNumber),
            IsActive = request.IsActive,
            CreatedBy = CurrentActor,
            UpdatedBy = CurrentActor
        };

        ApplyPreferences(user, request.Preferences);

        await repository.AddAsync(user, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
        return (await GetAsync(user.Id, false, cancellationToken))!;
    }

    public async Task<UserResponse?> UpdateAsync(string id, UserUpdateRequest request, CancellationToken cancellationToken)
    {
        var user = await repository.FindAsync(id, cancellationToken);
        if (user is null) return null;

        var email = NormalizeEmail(request.Email);
        var displayName = NormalizeRequired(request.DisplayName, nameof(request.DisplayName));
        await EnsureUniqueEmailAsync(email, id, cancellationToken);

        user.Email = email;
        user.DisplayName = displayName;
        user.FirstName = NormalizeOptional(request.FirstName);
        user.LastName = NormalizeOptional(request.LastName);
        user.PhoneNumber = NormalizeOptional(request.PhoneNumber);
        user.UpdatedBy = CurrentActor;

        if (request.Preferences is not null)
            ApplyPreferences(user, request.Preferences);

        await context.SaveChangesAsync(cancellationToken);
        return await GetAsync(id, false, cancellationToken);
    }

    public async Task<UserResponse?> PatchAsync(string id, UserPatchRequest request, CancellationToken cancellationToken)
    {
        var user = await repository.FindAsync(id, cancellationToken);
        if (user is null) return null;

        if (request.Email is not null)
        {
            var email = NormalizeEmail(request.Email);
            await EnsureUniqueEmailAsync(email, id, cancellationToken);
            user.Email = email;
        }

        if (request.DisplayName is not null)
            user.DisplayName = NormalizeRequired(request.DisplayName, nameof(request.DisplayName));

        if (request.FirstName is not null)
            user.FirstName = NormalizeOptional(request.FirstName);

        if (request.LastName is not null)
            user.LastName = NormalizeOptional(request.LastName);

        if (request.PhoneNumber is not null)
            user.PhoneNumber = NormalizeOptional(request.PhoneNumber);

        if (request.Preferences is not null)
            ApplyPreferencesPatch(user, request.Preferences);

        user.UpdatedBy = CurrentActor;
        await context.SaveChangesAsync(cancellationToken);
        return await GetAsync(id, false, cancellationToken);
    }

    public async Task<bool> DeleteAsync(string id, CancellationToken cancellationToken)
    {
        var user = await repository.FindAsync(id, cancellationToken);
        if (user is null) return false;

        user.MarkDeleted(CurrentActor, DateTimeOffset.UtcNow);
        user.UpdatedBy = CurrentActor;
        await context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<UserResponse?> RestoreAsync(string id, CancellationToken cancellationToken)
    {
        var user = await repository.FindAsync(id, cancellationToken, includeDeleted: true);
        if (user is null) return null;

        user.Restore();
        user.UpdatedBy = CurrentActor;
        await context.SaveChangesAsync(cancellationToken);
        return await GetAsync(id, false, cancellationToken);
    }

    public async Task<UserResponse?> SetActiveAsync(string id, bool active, CancellationToken cancellationToken)
    {
        var user = await repository.FindAsync(id, cancellationToken);
        if (user is null) return null;

        user.IsActive = active;
        user.UpdatedBy = CurrentActor;
        await context.SaveChangesAsync(cancellationToken);
        return await GetAsync(id, false, cancellationToken);
    }

    public async Task<UserPreferencesResponse?> GetPreferencesAsync(string id, CancellationToken cancellationToken)
    {
        var user = await repository.Query().AsNoTracking().FirstOrDefaultAsync(entity => entity.Id == id, cancellationToken);
        return user is null ? null : MapPreferences(user);
    }

    public async Task<UserPreferencesResponse?> UpdatePreferencesAsync(string id, UserPreferencesRequest request, CancellationToken cancellationToken)
    {
        var user = await repository.FindAsync(id, cancellationToken);
        if (user is null) return null;

        ApplyPreferences(user, request);
        user.UpdatedBy = CurrentActor;
        await context.SaveChangesAsync(cancellationToken);
        return MapPreferences(user);
    }

    public async Task<UserResponse?> GetMeAsync(UserActor actor, CancellationToken cancellationToken)
    {
        var user = await FindCurrentUserAsync(actor, includeDeleted: false, cancellationToken);
        return user is null ? null : MapResponse(user);
    }

    public async Task<UserResponse?> UpdateMeAsync(UserActor actor, UserMeUpdateRequest request, CancellationToken cancellationToken)
    {
        var user = await FindCurrentUserAsync(actor, includeDeleted: false, cancellationToken);
        if (user is null) return null;

        var email = NormalizeEmail(request.Email);
        var displayName = NormalizeRequired(request.DisplayName, nameof(request.DisplayName));
        await EnsureUniqueEmailAsync(email, user.Id, cancellationToken);

        user.Email = email;
        user.DisplayName = displayName;
        user.FirstName = NormalizeOptional(request.FirstName);
        user.LastName = NormalizeOptional(request.LastName);
        user.PhoneNumber = NormalizeOptional(request.PhoneNumber);
        user.UpdatedBy = CurrentActor;

        if (request.Preferences is not null)
            ApplyPreferences(user, request.Preferences);

        await context.SaveChangesAsync(cancellationToken);
        return await GetAsync(user.Id, false, cancellationToken);
    }

    public async Task<IReadOnlyList<UserResponse>> BatchGetAsync(IReadOnlyCollection<string> ids, bool includeDeleted, CancellationToken cancellationToken)
    {
        if (ids.Count == 0) return [];

        var normalizedIds = ids.Select(id => id.Trim()).Where(id => id.Length > 0).Distinct().ToArray();
        var users = BuildUserQuery(new UserListQuery(IncludeDeleted: includeDeleted))
            .Where(user => normalizedIds.Contains(user.Id));

        return await users.ToListAsync(cancellationToken);
    }

    public async Task<UserProfileSummaryResponse?> GetProfileSummaryAsync(string id, CancellationToken cancellationToken)
    {
        var user = await repository.Query().AsNoTracking().FirstOrDefaultAsync(entity => entity.Id == id, cancellationToken);
        if (user is null) return null;

        return new UserProfileSummaryResponse(user.Id, user.ExternalAuthUserId, user.Email, user.DisplayName, user.IsActive);
    }

    private IQueryable<UserResponse> BuildUserQuery(UserListQuery query, string? id = null)
    {
        var users = repository.Query(query.IncludeDeleted).AsNoTracking();
        users = ServiceGuards.ApplySearch(users, query.Search,
            entity => entity.ExternalAuthUserId,
            entity => entity.Email,
            entity => entity.DisplayName,
            entity => entity.FirstName,
            entity => entity.LastName,
            entity => entity.PhoneNumber);

        if (!string.IsNullOrWhiteSpace(id))
            users = users.Where(entity => entity.Id == id);

        if (query.IsActive is not null)
            users = users.Where(entity => entity.IsActive == query.IsActive.Value);

        return users
            .OrderByDescending(entity => entity.UpdatedAt)
            .Select(MapProjection());
    }

    private static Expression<Func<UserProfile, UserResponse>> MapProjection()
    {
        return entity => new UserResponse(
            entity.Id,
            entity.TenantId,
            entity.ExternalAuthUserId,
            entity.Email,
            entity.DisplayName,
            entity.FirstName,
            entity.LastName,
            entity.PhoneNumber,
            entity.IsActive,
            new UserPreferencesResponse(
                entity.PreferencesLanguage,
                entity.PreferencesTimeZone,
                ToDtoTheme(entity.PreferencesTheme),
                entity.PreferencesEmailNotificationsEnabled,
                entity.PreferencesSmsNotificationsEnabled),
            entity.IsDeleted,
            entity.DeletedAt,
            entity.DeletedBy,
            entity.CreatedAt,
            entity.UpdatedAt,
            entity.CreatedBy,
            entity.UpdatedBy);
    }

    private static UserResponse MapResponse(UserProfile entity)
    {
        return new UserResponse(
            entity.Id,
            entity.TenantId,
            entity.ExternalAuthUserId,
            entity.Email,
            entity.DisplayName,
            entity.FirstName,
            entity.LastName,
            entity.PhoneNumber,
            entity.IsActive,
            MapPreferences(entity),
            entity.IsDeleted,
            entity.DeletedAt,
            entity.DeletedBy,
            entity.CreatedAt,
            entity.UpdatedAt,
            entity.CreatedBy,
            entity.UpdatedBy);
    }

    private static UserPreferencesResponse MapPreferences(UserProfile entity)
    {
        return new UserPreferencesResponse(
            entity.PreferencesLanguage,
            entity.PreferencesTimeZone,
            ToDtoTheme(entity.PreferencesTheme),
            entity.PreferencesEmailNotificationsEnabled,
            entity.PreferencesSmsNotificationsEnabled);
    }

    private static UserTheme ToDtoTheme(UserThemePreference theme)
    {
        return theme switch
        {
            UserThemePreference.System => UserTheme.System,
            UserThemePreference.Light => UserTheme.Light,
            UserThemePreference.Dark => UserTheme.Dark,
            _ => throw new InvalidOperationException("Unsupported user theme.")
        };
    }

    private static UserThemePreference ToModelTheme(UserTheme theme)
    {
        return theme switch
        {
            UserTheme.System => UserThemePreference.System,
            UserTheme.Light => UserThemePreference.Light,
            UserTheme.Dark => UserThemePreference.Dark,
            _ => throw new InvalidOperationException("Unsupported user theme.")
        };
    }

    private async Task<UserProfile?> FindCurrentUserAsync(UserActor actor, bool includeDeleted, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(actor.InternalUserId))
            return await repository.FindAsync(actor.InternalUserId!, cancellationToken, includeDeleted);

        if (!string.IsNullOrWhiteSpace(actor.ExternalAuthUserId))
        {
            return await repository.Query(includeDeleted)
                .FirstOrDefaultAsync(entity => entity.ExternalAuthUserId == actor.ExternalAuthUserId, cancellationToken);
        }

        throw new InvalidOperationException("Unable to resolve current user.");
    }

    private async Task EnsureUniqueExternalAuthUserIdAsync(string externalAuthUserId, string? userId, CancellationToken cancellationToken)
    {
        await ServiceGuards.EnsureDoesNotExistAsync(
            context.Users,
            entity => entity.ExternalAuthUserId == externalAuthUserId && entity.Id != userId,
            $"A user with externalAuthUserId '{externalAuthUserId}' already exists.",
            cancellationToken);
    }

    private async Task EnsureUniqueEmailAsync(string email, string? userId, CancellationToken cancellationToken)
    {
        await ServiceGuards.EnsureDoesNotExistAsync(
            context.Users,
            entity => entity.Email == email && entity.Id != userId,
            $"A user with email '{email}' already exists.",
            cancellationToken);
    }

    private static void ApplyPreferences(UserProfile user, UserPreferencesRequest? request)
    {
        var normalized = request ?? new UserPreferencesRequest();
        user.PreferencesLanguage = NormalizeRequired(normalized.Language, nameof(normalized.Language)).ToLowerInvariant();
        user.PreferencesTimeZone = NormalizeRequired(normalized.TimeZone, nameof(normalized.TimeZone));
        user.PreferencesTheme = ToModelTheme(normalized.Theme);
        user.PreferencesEmailNotificationsEnabled = normalized.EmailNotificationsEnabled;
        user.PreferencesSmsNotificationsEnabled = normalized.SmsNotificationsEnabled;
    }

    private static void ApplyPreferencesPatch(UserProfile user, UserPreferencesPatchRequest request)
    {
        if (request.Language is not null)
            user.PreferencesLanguage = NormalizeRequired(request.Language, nameof(request.Language)).ToLowerInvariant();

        if (request.TimeZone is not null)
            user.PreferencesTimeZone = NormalizeRequired(request.TimeZone, nameof(request.TimeZone));

        if (request.Theme is not null)
            user.PreferencesTheme = ToModelTheme(request.Theme.Value);

        if (request.EmailNotificationsEnabled is not null)
            user.PreferencesEmailNotificationsEnabled = request.EmailNotificationsEnabled.Value;

        if (request.SmsNotificationsEnabled is not null)
            user.PreferencesSmsNotificationsEnabled = request.SmsNotificationsEnabled.Value;
    }

    private static string NormalizeRequired(string? value, string fieldName)
    {
        var normalized = value?.Trim();
        if (string.IsNullOrWhiteSpace(normalized))
            throw new InvalidOperationException($"{fieldName} is required.");

        return normalized;
    }

    private static string NormalizeEmail(string? value)
    {
        var normalized = NormalizeRequired(value, "email").ToLowerInvariant();
        if (!normalized.Contains('@'))
            throw new InvalidOperationException("email must be a valid email address.");

        return normalized;
    }

    private static string? NormalizeOptional(string? value)
    {
        var normalized = value?.Trim();
        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
    }

    private string CurrentActor => string.IsNullOrWhiteSpace(tenantContextAccessor.Current?.UserId) ? SystemUser : tenantContextAccessor.Current!.UserId;
}
