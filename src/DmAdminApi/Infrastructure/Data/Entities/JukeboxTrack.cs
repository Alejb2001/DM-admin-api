namespace DmAdminApi.Infrastructure.Data.Entities;

public class JukeboxTrack
{
    public Guid Id { get; set; }
    public Guid CampaignId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public Campaign Campaign { get; set; } = null!;
}
