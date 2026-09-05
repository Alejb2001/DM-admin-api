namespace DmAdminApi.Infrastructure.Data.Entities;

public class CampaignRole
{
    public Guid Id { get; set; }
    public Guid CampaignId { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsSystemDefault { get; set; }

    public Campaign Campaign { get; set; } = null!;
    public ICollection<CampaignMember> Members { get; set; } = [];
    public ICollection<EntityPermission> EntityPermissions { get; set; } = [];
}

public static class SystemRoles
{
    public const string CoDm = "Co-DM";
    public const string Player = "Player";
    public const string Spectator = "Spectator";
}
