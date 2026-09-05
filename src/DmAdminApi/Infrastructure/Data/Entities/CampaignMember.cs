namespace DmAdminApi.Infrastructure.Data.Entities;

public class CampaignMember
{
    public Guid Id { get; set; }
    public Guid CampaignId { get; set; }
    public Guid UserId { get; set; }
    public Guid RoleId { get; set; }
    public DateTime JoinedAt { get; set; }

    public Campaign Campaign { get; set; } = null!;
    public User User { get; set; } = null!;
    public CampaignRole Role { get; set; } = null!;
}
