using System.ComponentModel.DataAnnotations;

namespace DmAdminApi.Features.VttAdvanced.Dtos;

// ── Fog of War ────────────────────────────────────────────────────────────────

public record FogZoneDto(
    Guid Id,
    Guid SceneId,
    string Shape,
    float X,
    float Y,
    float? Width,
    float? Height,
    float? Radius
);

public record CreateFogZoneDto(
    [Required] string Shape,
    float X,
    float Y,
    float? Width,
    float? Height,
    float? Radius
);

public record ToggleFogDto(bool FogEnabled);

// ── Initiative ────────────────────────────────────────────────────────────────

public record InitiativeEntryDto(
    Guid Id,
    Guid SessionId,
    Guid? TokenId,
    string Name,
    int Initiative,
    int SortOrder,
    bool IsActive
);

public record CreateInitiativeEntryDto(
    Guid? TokenId,
    [Required, MaxLength(200)] string Name,
    int Initiative
);

public record UpdateInitiativeOrderDto(List<Guid> OrderedIds);

// ── Token Conditions ──────────────────────────────────────────────────────────

public record AddConditionDto([Required, MaxLength(50)] string Condition);
