using System.Text.Json;

namespace DmAdminApi.Infrastructure.Data.Entities;

public class WorldEntity
{
    public Guid Id { get; set; }
    public Guid CampaignId { get; set; }
    public Guid EntityTypeId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public Guid CreatedBy { get; set; }
    public JsonDocument? CustomFields { get; set; }   // JSONB column
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }           // Used for optimistic concurrency

    public Campaign Campaign { get; set; } = null!;
    public EntityType EntityType { get; set; } = null!;
    public User Creator { get; set; } = null!;
    public ICollection<EntityPermission> Permissions { get; set; } = [];
}
