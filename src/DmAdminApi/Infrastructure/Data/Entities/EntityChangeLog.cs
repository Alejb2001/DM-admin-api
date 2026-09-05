namespace DmAdminApi.Infrastructure.Data.Entities;

/// <summary>Append-only log of changes made to world entities (Master plan feature).</summary>
public class EntityChangeLog
{
    public Guid Id { get; set; }
    public Guid EntityId { get; set; }
    public Guid UserId { get; set; }
    public string UserDisplayName { get; set; } = string.Empty;  // Denormalized for display
    public DateTime ChangedAt { get; set; }
    public string? FieldChanged { get; set; }   // "name" | "customFields" | null (= created)
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }

    public WorldEntity Entity { get; set; } = null!;
    public User ChangedBy { get; set; } = null!;
}
