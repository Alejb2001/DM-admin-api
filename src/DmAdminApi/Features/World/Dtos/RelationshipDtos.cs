using System.ComponentModel.DataAnnotations;

namespace DmAdminApi.Features.World.Dtos;

public record RelationshipTypeDto(
    Guid Id,
    Guid CampaignId,
    string LabelForward,
    string LabelInverse,
    Guid? SourceTypeId,
    Guid? TargetTypeId
);

public record CreateRelationshipTypeDto(
    [Required, MaxLength(100)] string LabelForward,
    [Required, MaxLength(100)] string LabelInverse,
    Guid? SourceTypeId,
    Guid? TargetTypeId
);

public record UpdateRelationshipTypeDto(
    [Required, MaxLength(100)] string LabelForward,
    [Required, MaxLength(100)] string LabelInverse,
    Guid? SourceTypeId,
    Guid? TargetTypeId
);

public record EntityRelationshipDto(
    Guid Id,
    Guid SourceEntityId,
    string SourceEntityName,
    Guid TargetEntityId,
    string TargetEntityName,
    string TargetEntityTypeName,
    string? TargetEntityTypeIcon,
    string? TargetEntityTypeColor,
    Guid RelationshipTypeId,
    string LabelForward,
    string LabelInverse,
    string? Notes
);

public record CreateEntityRelationshipDto(
    [Required] Guid TargetEntityId,
    [Required] Guid RelationshipTypeId,
    string? Notes
);

// For the graph view
public record GraphNodeDto(Guid Id, string Name, string EntityTypeName, string? Icon, string? Color);
public record GraphEdgeDto(Guid Id, Guid Source, Guid Target, string Label);
public record GraphDto(List<GraphNodeDto> Nodes, List<GraphEdgeDto> Edges);
