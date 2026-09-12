using System.ComponentModel.DataAnnotations;

namespace DmAdminApi.Features.CharacterSheets.Dtos;

public record CharacterResourceDto(
    Guid Id,
    Guid EntityId,
    string Name,
    int Current,
    int Max,
    string Color,
    int SortOrder
);

public record CreateCharacterResourceDto(
    [Required, MaxLength(100)] string Name,
    int Current,
    int Max,
    [MaxLength(20)] string Color,
    int SortOrder
);

public record UpdateResourceValueDto(int Current);
