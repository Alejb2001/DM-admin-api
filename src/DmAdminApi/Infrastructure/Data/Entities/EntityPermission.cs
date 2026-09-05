namespace DmAdminApi.Infrastructure.Data.Entities;

public class EntityPermission
{
    public Guid Id { get; set; }
    public Guid EntityId { get; set; }
    public Guid RoleId { get; set; }
    public bool CanView { get; set; }
    public bool CanEdit { get; set; }

    public WorldEntity Entity { get; set; } = null!;
    public CampaignRole Role { get; set; } = null!;
}
