namespace UAM.Models;

public enum VisibilityLevel
{
    Everyone,
    Followers,
    Nobody
}

public sealed class UserPrivacySettings : BaseModel
{
    public string UserProfileId { get; set; } = string.Empty;

    /// <summary>Who can view this user's profile (Everyone / Followers / Nobody).</summary>
    public VisibilityLevel ProfileVisibility { get; set; } = VisibilityLevel.Everyone;

    /// <summary>Who can send this user direct messages.</summary>
    public VisibilityLevel WhoCanMessage { get; set; } = VisibilityLevel.Everyone;

    /// <summary>Who can @mention this user.</summary>
    public VisibilityLevel WhoCanMention { get; set; } = VisibilityLevel.Everyone;

    /// <summary>Whether this profile may be indexed by search engines and the platform search.</summary>
    public bool AllowIndexing { get; set; } = true;

    /// <summary>Whether NSFW content can appear in this user's feed.</summary>
    public bool AllowNsfwInFeed { get; set; }
}
