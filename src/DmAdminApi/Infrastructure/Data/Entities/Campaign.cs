namespace DmAdminApi.Infrastructure.Data.Entities;

public class Campaign
{
    public Guid Id { get; set; }
    public Guid OwnerId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string JoinCode { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }

    public User Owner { get; set; } = null!;
    public ICollection<CampaignRole> Roles { get; set; } = [];
    public ICollection<CampaignMember> Members { get; set; } = [];
    public ICollection<CampaignInvitation> Invitations { get; set; } = [];
    public ICollection<WorldEntity> Entities { get; set; } = [];
}
