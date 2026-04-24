namespace UAM.Models;

public enum UserThemePreference
{
    System,
    Light,
    Dark
}

public sealed class UserProfile : BaseModel
{
    public string ExternalAuthUserId { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;

    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? PhoneNumber { get; set; }

    public bool IsActive { get; set; } = true;

    public string PreferencesLanguage { get; set; } = "en";
    public string PreferencesTimeZone { get; set; } = "UTC";
    public UserThemePreference PreferencesTheme { get; set; } = UserThemePreference.System;
    public bool PreferencesEmailNotificationsEnabled { get; set; } = true;
    public bool PreferencesSmsNotificationsEnabled { get; set; }

    // Social profile extensions (Phase 1)
    public string? Handle { get; set; }
    public string? Bio { get; set; }
    public string? AvatarFileId { get; set; }
    public string? CoverFileId { get; set; }
    public string? Website { get; set; }
    public string? LinksJson { get; set; }
    public string? PronounsJson { get; set; }
    public bool IsVerified { get; set; }
    public string? VerifiedBadgeKind { get; set; }
}
