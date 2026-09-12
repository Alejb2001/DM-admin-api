using System.ComponentModel.DataAnnotations;

namespace DmAdminApi.Features.Maps.Dtos;

public record TokenConditionInfo(Guid Id, string Condition);

public record SceneDto(
    Guid Id,
    Guid SessionId,
    string Name,
    string? BackgroundUrl,
    int GridSize,
    bool GridEnabled,
    bool IsActive,
    bool FogEnabled
);

public record CreateSceneDto(
    [Required, MaxLength(200)] string Name,
    string? BackgroundUrl,
    int GridSize = 50,
    bool GridEnabled = true
);

public record UpdateSceneDto(
    [Required, MaxLength(200)] string Name,
    string? BackgroundUrl,
    int GridSize,
    bool GridEnabled
);

public record MapTokenDto(
    Guid Id,
    Guid SceneId,
    Guid? EntityId,
    string Label,
    string? ImageUrl,
    string Color,
    double X,
    double Y,
    int Width,
    int Height,
    bool IsVisible,
    Guid? ControlledBy,
    List<TokenConditionInfo> Conditions
);

public record CreateTokenDto(
    Guid? EntityId,
    [Required, MaxLength(100)] string Label,
    string? ImageUrl,
    string Color,
    double X,
    double Y,
    int Width,
    int Height,
    Guid? ControlledBy
);

public record MoveTokenDto(double X, double Y);

public record UpdateTokenDto(
    [Required, MaxLength(100)] string Label,
    string? ImageUrl,
    [Required] string Color,
    int Width,
    int Height,
    bool IsVisible,
    Guid? ControlledBy
);
