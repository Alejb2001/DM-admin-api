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
    List<TokenConditionInfo> Conditions,
    int? Bar1Value,
    int? Bar1Max,
    string? Bar1Color,
    int? Bar2Value,
    int? Bar2Max,
    string? Bar2Color,
    double? AuraRadius,
    string? AuraColor,
    double AuraOpacity,
    double Rotation,
    bool IsLocked
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

public record AnnotationDto(
    Guid Id,
    Guid SceneId,
    string Type,
    double[] Points,
    string Color,
    int StrokeWidth,
    string? FillColor,
    double FillOpacity,
    string? Text,
    int FontSize,
    Guid CreatedBy,
    DateTimeOffset CreatedAt
);

public record CreateAnnotationDto(
    [Required, MaxLength(20)] string Type,
    [Required] double[] Points,
    [Required, MaxLength(20)] string Color,
    int StrokeWidth = 2,
    string? FillColor = null,
    double FillOpacity = 0,
    [MaxLength(500)] string? Text = null,
    int FontSize = 16
);

public record UpdateTokenDto(
    [Required, MaxLength(100)] string Label,
    string? ImageUrl,
    [Required] string Color,
    int Width,
    int Height,
    bool IsVisible,
    Guid? ControlledBy,
    int? Bar1Value,
    int? Bar1Max,
    string? Bar1Color,
    int? Bar2Value,
    int? Bar2Max,
    string? Bar2Color,
    double? AuraRadius,
    string? AuraColor,
    double AuraOpacity,
    double Rotation,
    bool IsLocked
);
