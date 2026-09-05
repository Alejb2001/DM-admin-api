namespace DmAdminApi.Infrastructure.Data.Entities;

public class RelationshipType
{
    public Guid Id { get; set; }
    public Guid CampaignId { get; set; }
    public string LabelForward { get; set; } = string.Empty;   // e.g. "gobierna"
    public string LabelInverse { get; set; } = string.Empty;   // e.g. "es gobernado por"
    public Guid? SourceTypeId { get; set; }   // null = any type
    public Guid? TargetTypeId { get; set; }   // null = any type

    public Campaign Campaign { get; set; } = null!;
    public EntityType? SourceType { get; set; }
    public EntityType? TargetType { get; set; }
    public ICollection<EntityRelationship> Relationships { get; set; } = [];
}
