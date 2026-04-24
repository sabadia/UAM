using UAM.Dtos.Common;

namespace UAM.Dtos.Users;

public enum UserTheme
{
    System,
    Light,
    Dark
}

public enum PrivacyVisibilityLevel
{
    Everyone,
    Followers,
    Nobody
}

public sealed record UserListQuery(
    int Offset = 0,
    int Limit = 20,
    string? Search = null,
    bool? IsActive = null,
    bool IncludeDeleted = false,
    string? SortBy = null,
    string SortDirection = "desc")
{
    public OffsetPaginationQuery Pagination => new(Offset, Limit, Search, SortBy, SortDirection);
}

public sealed record UserPreferencesRequest(
    string Language = "en",
    string TimeZone = "UTC",
    UserTheme Theme = UserTheme.System,
    bool EmailNotificationsEnabled = true,
    bool SmsNotificationsEnabled = false);

public sealed record UserPreferencesPatchRequest(
    string? Language = null,
    string? TimeZone = null,
    UserTheme? Theme = null,
    bool? EmailNotificationsEnabled = null,
    bool? SmsNotificationsEnabled = null);

public sealed record UserCreateRequest(
    string ExternalAuthUserId,
    string Email,
    string DisplayName,
    string? FirstName = null,
    string? LastName = null,
    string? PhoneNumber = null,
    bool IsActive = true,
    UserPreferencesRequest? Preferences = null);

public sealed record UserUpdateRequest(
    string Email,
    string DisplayName,
    string? FirstName = null,
    string? LastName = null,
    string? PhoneNumber = null,
    UserPreferencesRequest? Preferences = null);

public sealed record UserPatchRequest(
    string? Email = null,
    string? DisplayName = null,
    string? FirstName = null,
    string? LastName = null,
    string? PhoneNumber = null,
    UserPreferencesPatchRequest? Preferences = null);

public sealed record UserMeUpdateRequest(
    string Email,
    string DisplayName,
    string? FirstName = null,
    string? LastName = null,
    string? PhoneNumber = null,
    UserPreferencesRequest? Preferences = null);

public sealed record UserMeUpdatePatchRequest(
    string? Email = null,
    string? DisplayName = null,
    string? FirstName = null,
    string? LastName = null,
    string? PhoneNumber = null,
    string? Handle = null,
    string? Bio = null,
    string? Website = null,
    string? AvatarFileId = null,
    string? CoverFileId = null,
    string? LinksJson = null,
    string? PronounsJson = null,
    UserPreferencesPatchRequest? Preferences = null);

public sealed record UserPreferencesResponse(
    string Language,
    string TimeZone,
    UserTheme Theme,
    bool EmailNotificationsEnabled,
    bool SmsNotificationsEnabled);

public sealed record UserPrivacySettingsPatchRequest(
    PrivacyVisibilityLevel? ProfileVisibility = null,
    PrivacyVisibilityLevel? WhoCanMessage = null,
    PrivacyVisibilityLevel? WhoCanMention = null,
    bool? AllowIndexing = null,
    bool? AllowNsfwInFeed = null);

public sealed record UserPrivacySettingsResponse(
    string UserProfileId,
    PrivacyVisibilityLevel ProfileVisibility,
    PrivacyVisibilityLevel WhoCanMessage,
    PrivacyVisibilityLevel WhoCanMention,
    bool AllowIndexing,
    bool AllowNsfwInFeed);

public sealed record UserResponse(
    string Id,
    string TenantId,
    string ExternalAuthUserId,
    string Email,
    string DisplayName,
    string? FirstName,
    string? LastName,
    string? PhoneNumber,
    bool IsActive,
    UserPreferencesResponse Preferences,
    bool IsDeleted,
    DateTimeOffset? DeletedAt,
    string? DeletedBy,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    string CreatedBy,
    string UpdatedBy,
    string? Handle = null,
    string? Bio = null,
    string? AvatarFileId = null,
    string? CoverFileId = null,
    string? Website = null,
    string? LinksJson = null,
    string? PronounsJson = null,
    bool IsVerified = false,
    string? VerifiedBadgeKind = null);

public sealed record UserProfileSummaryResponse(
    string Id,
    string ExternalAuthUserId,
    string Email,
    string DisplayName,
    bool IsActive);
