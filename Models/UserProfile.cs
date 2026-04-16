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
}
