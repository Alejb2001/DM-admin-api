namespace DmAdminApi.Infrastructure.Data.Entities;

public class EntityRelationship
{
    public Guid Id { get; set; }
    public Guid SourceEntityId { get; set; }
    public Guid TargetEntityId { get; set; }
    public Guid RelationshipTypeId { get; set; }
    public string? Notes { get; set; }

    public WorldEntity SourceEntity { get; set; } = null!;
    public WorldEntity TargetEntity { get; set; } = null!;
    public RelationshipType RelationshipType { get; set; } = null!;
}
