namespace DmAdminApi.Infrastructure.Data.Entities;

public class CampaignInvitation
{
    public Guid Id { get; set; }
    public Guid CampaignId { get; set; }
    public Guid RoleId { get; set; }
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public bool IsUsed { get; set; }
    public DateTime CreatedAt { get; set; }

    public Campaign Campaign { get; set; } = null!;
    public CampaignRole Role { get; set; } = null!;
}
