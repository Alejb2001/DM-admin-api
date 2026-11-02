namespace DmAdminApi.Infrastructure.Data.Entities;

public class Handout
{
    public Guid Id { get; set; }
    public Guid CampaignId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Type { get; set; } = "text"; // text | image | link
    public string Content { get; set; } = string.Empty;
    public bool IsRevealed { get; set; } = false;
    public Guid CreatedBy { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public Campaign Campaign { get; set; } = null!;
    public User Creator { get; set; } = null!;
}
