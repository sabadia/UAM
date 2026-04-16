namespace UAM.Models;

public class Content : BaseModel
{
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Value { get; set; }

    public bool IsPublished { get; set; }
    public DateTimeOffset? PublishedAt { get; set; }

    public int ViewCount { get; set; }
    public int LikeCount { get; set; }
    public int DislikeCount { get; set; }
}
