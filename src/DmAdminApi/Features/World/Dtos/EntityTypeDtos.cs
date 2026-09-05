using System.ComponentModel.DataAnnotations;

namespace DmAdminApi.Features.World.Dtos;

public record EntityTypeFieldDto(
    Guid Id,
    string Name,
    string FieldType,
    bool IsRequired,
    int SortOrder
);

public record EntityTypeWithFieldsDto(
    Guid Id,
    string Name,
    string? Icon,
    string? Color,
    bool IsSystemDefault,
    Guid? CampaignId,
    List<EntityTypeFieldDto> Fields
);

public record CreateEntityTypeDto(
    [Required, MaxLength(100)] string Name,
    [MaxLength(50)] string? Icon,
    [MaxLength(20)] string? Color
);

public record UpdateEntityTypeDto(
    [Required, MaxLength(100)] string Name,
    [MaxLength(50)] string? Icon,
    [MaxLength(20)] string? Color
);

public record CreateEntityTypeFieldDto(
    [Required, MaxLength(100)] string Name,
    [Required] string FieldType,   // text|number|date|boolean|reference|richtext|url
    bool IsRequired,
    int SortOrder
);

public record UpdateEntityTypeFieldDto(
    [Required, MaxLength(100)] string Name,
    [Required] string FieldType,
    bool IsRequired,
    int SortOrder
);
