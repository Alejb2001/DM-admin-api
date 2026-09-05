using System.Text.Json;

namespace DmAdminApi.Features.World.Dtos;

// ── History ──────────────────────────────────────────────────────────────────

public record EntityChangeLogDto(
    Guid Id,
    Guid UserId,
    string UserDisplayName,
    DateTime ChangedAt,
    string? FieldChanged,
    string? OldValue,
    string? NewValue
);

// ── Export ────────────────────────────────────────────────────────────────────

public record CampaignExportDto(
    Guid Id,
    string Name,
    string? Description,
    DateTime CreatedAt,
    string ExportedAt,
    List<EntityTypeExportDto> EntityTypes,
    List<EntityExportDto> Entities,
    List<RelationshipTypeExportDto> RelationshipTypes,
    List<EntityRelationshipExportDto> Relationships
);

public record EntityTypeExportDto(
    Guid Id,
    string Name,
    string? Icon,
    string? Color,
    bool IsSystemDefault,
    List<EntityTypeFieldExportDto> Fields
);

public record EntityTypeFieldExportDto(
    Guid Id,
    string Name,
    string FieldType,
    bool IsRequired,
    int SortOrder
);

public record EntityExportDto(
    Guid Id,
    Guid EntityTypeId,
    string EntityTypeName,
    string Name,
    string Slug,
    JsonDocument? CustomFields,
    DateTime CreatedAt,
    DateTime UpdatedAt
);

public record RelationshipTypeExportDto(
    Guid Id,
    string LabelForward,
    string LabelInverse,
    Guid? SourceTypeId,
    Guid? TargetTypeId
);

public record EntityRelationshipExportDto(
    Guid Id,
    Guid SourceEntityId,
    string SourceEntityName,
    Guid TargetEntityId,
    string TargetEntityName,
    Guid RelationshipTypeId,
    string LabelForward,
    string? Notes
);
