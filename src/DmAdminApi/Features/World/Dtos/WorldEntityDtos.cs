using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace DmAdminApi.Features.World.Dtos;

public record CreateWorldEntityDto(
    [Required] Guid EntityTypeId,
    [Required, MaxLength(200)] string Name,
    JsonDocument? CustomFields
);

public record UpdateWorldEntityDto(
    [Required, MaxLength(200)] string Name,
    JsonDocument? CustomFields,
    DateTime UpdatedAt   // For optimistic concurrency check
);

public record WorldEntityDto(
    Guid Id,
    Guid CampaignId,
    Guid EntityTypeId,
    string EntityTypeName,
    string? EntityTypeIcon,
    string? EntityTypeColor,
    List<EntityTypeFieldDto> EntityTypeFields,
    string Name,
    string Slug,
    Guid CreatedBy,
    JsonDocument? CustomFields,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    List<PermissionDto> Permissions
);

public record WorldEntitySummaryDto(
    Guid Id,
    Guid EntityTypeId,
    string EntityTypeName,
    string? EntityTypeIcon,
    string? EntityTypeColor,
    string Name,
    string Slug,
    DateTime UpdatedAt
);

public record PermissionDto(
    Guid RoleId,
    string RoleName,
    bool CanView,
    bool CanEdit
);

public record SetPermissionDto(
    [Required] Guid RoleId,
    bool CanView,
    bool CanEdit
);

// Basic entity type DTO (used for listing in world)
public record EntityTypeDto(
    Guid Id,
    string Name,
    string? Icon,
    string? Color,
    bool IsSystemDefault,
    Guid? CampaignId
);

// Search result DTO
public record EntitySearchResultDto(
    Guid Id,
    Guid EntityTypeId,
    string EntityTypeName,
    string? EntityTypeIcon,
    string? EntityTypeColor,
    string Name,
    string Slug,
    DateTime UpdatedAt
);
