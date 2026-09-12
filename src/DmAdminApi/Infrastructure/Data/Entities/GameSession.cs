namespace DmAdminApi.Infrastructure.Data.Entities;

public class GameSession
{
    public Guid Id { get; set; }
    public Guid CampaignId { get; set; }
    public string Name { get; set; } = null!;
    public Guid CreatedById { get; set; }
    public DateTimeOffset StartedAt { get; set; }
    public DateTimeOffset? EndedAt { get; set; }
    public string Status { get; set; } = "active"; // "active" | "ended"

    // Navigation
    public Campaign Campaign { get; set; } = null!;
    public User CreatedBy { get; set; } = null!;
    public ICollection<ChatMessage> Messages { get; set; } = [];
}
