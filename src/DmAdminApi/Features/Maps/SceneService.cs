using System.Text.Json;
using DmAdminApi.Features.Maps.Dtos;
using DmAdminApi.Infrastructure.Data;
using DmAdminApi.Infrastructure.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace DmAdminApi.Features.Maps;

public class SceneService(AppDbContext db)
{
    // ── Scenes ────────────────────────────────────────────────────────────────

    public async Task<List<SceneDto>> GetScenesAsync(Guid sessionId)
    {
        return await db.SessionScenes
            .Where(s => s.SessionId == sessionId)
            .OrderBy(s => s.CreatedAt)
            .Select(s => ToSceneDto(s))
            .ToListAsync();
    }

    public async Task<SceneDto> CreateSceneAsync(Guid sessionId, CreateSceneDto dto)
    {
        var scene = new SessionScene
        {
            Id = Guid.NewGuid(),
            SessionId = sessionId,
            Name = dto.Name,
            BackgroundUrl = dto.BackgroundUrl,
            GridSize = dto.GridSize > 0 ? dto.GridSize : 50,
            GridEnabled = dto.GridEnabled,
            IsActive = false,
            CreatedAt = DateTimeOffset.UtcNow
        };
        db.SessionScenes.Add(scene);
        await db.SaveChangesAsync();
        return ToSceneDto(scene);
    }

    public async Task<SceneDto> UpdateSceneAsync(Guid sceneId, UpdateSceneDto dto)
    {
        var scene = await db.SessionScenes.FindAsync(sceneId)
            ?? throw new KeyNotFoundException("Escena no encontrada.");

        scene.Name = dto.Name;
        scene.BackgroundUrl = dto.BackgroundUrl;
        scene.GridSize = dto.GridSize > 0 ? dto.GridSize : 50;
        scene.GridEnabled = dto.GridEnabled;

        await db.SaveChangesAsync();
        return ToSceneDto(scene);
    }

    public async Task<SceneDto> ActivateSceneAsync(Guid sceneId, Guid sessionId)
    {
        await db.SessionScenes
            .Where(s => s.SessionId == sessionId && s.IsActive)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.IsActive, false));

        var scene = await db.SessionScenes.FindAsync(sceneId)
            ?? throw new KeyNotFoundException("Escena no encontrada.");

        scene.IsActive = true;
        await db.SaveChangesAsync();
        return ToSceneDto(scene);
    }

    public async Task DeleteSceneAsync(Guid sceneId)
    {
        var scene = await db.SessionScenes.FindAsync(sceneId)
            ?? throw new KeyNotFoundException("Escena no encontrada.");

        db.SessionScenes.Remove(scene);
        await db.SaveChangesAsync();
    }

    // ── Tokens ────────────────────────────────────────────────────────────────

    public async Task<List<MapTokenDto>> GetTokensAsync(Guid sceneId)
    {
        var tokens = await db.MapTokens
            .Include(t => t.Conditions)
            .Where(t => t.SceneId == sceneId)
            .OrderBy(t => t.UpdatedAt)
            .ToListAsync();
        return tokens.Select(ToTokenDto).ToList();
    }

    public async Task<MapTokenDto> AddTokenAsync(Guid sceneId, CreateTokenDto dto)
    {
        var token = new MapToken
        {
            Id = Guid.NewGuid(),
            SceneId = sceneId,
            EntityId = dto.EntityId,
            Label = dto.Label,
            ImageUrl = dto.ImageUrl,
            Color = string.IsNullOrEmpty(dto.Color) ? "#7E57C2" : dto.Color,
            X = dto.X,
            Y = dto.Y,
            Width = dto.Width > 0 ? dto.Width : 1,
            Height = dto.Height > 0 ? dto.Height : 1,
            IsVisible = true,
            ControlledBy = dto.ControlledBy,
            UpdatedAt = DateTimeOffset.UtcNow
        };
        db.MapTokens.Add(token);
        await db.SaveChangesAsync();
        return ToTokenDto(token);
    }

    public async Task<MapTokenDto> MoveTokenAsync(Guid tokenId, MoveTokenDto dto, Guid userId, bool isDm)
    {
        var token = await db.MapTokens.Include(t => t.Conditions).FirstOrDefaultAsync(t => t.Id == tokenId)
            ?? throw new KeyNotFoundException("Token no encontrado.");

        if (!isDm && token.ControlledBy != userId)
            throw new UnauthorizedAccessException("No tienes permiso para mover este token.");

        token.X = dto.X;
        token.Y = dto.Y;
        token.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync();
        return ToTokenDto(token);
    }

    public async Task<MapTokenDto> UpdateTokenAsync(Guid tokenId, UpdateTokenDto dto)
    {
        var token = await db.MapTokens.Include(t => t.Conditions).FirstOrDefaultAsync(t => t.Id == tokenId)
            ?? throw new KeyNotFoundException("Token no encontrado.");

        token.Label = dto.Label;
        token.ImageUrl = dto.ImageUrl;
        token.Color = dto.Color;
        token.Width = dto.Width > 0 ? dto.Width : 1;
        token.Height = dto.Height > 0 ? dto.Height : 1;
        token.IsVisible = dto.IsVisible;
        token.ControlledBy = dto.ControlledBy;
        token.Bar1Value = dto.Bar1Value;
        token.Bar1Max = dto.Bar1Max;
        token.Bar1Color = dto.Bar1Color;
        token.Bar2Value = dto.Bar2Value;
        token.Bar2Max = dto.Bar2Max;
        token.Bar2Color = dto.Bar2Color;
        token.AuraRadius = dto.AuraRadius;
        token.AuraColor = dto.AuraColor;
        token.AuraOpacity = dto.AuraOpacity;
        token.Rotation = dto.Rotation;
        token.IsLocked = dto.IsLocked;
        token.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync();
        return ToTokenDto(token);
    }

    public async Task DeleteTokenAsync(Guid tokenId)
    {
        var token = await db.MapTokens.FindAsync(tokenId)
            ?? throw new KeyNotFoundException("Token no encontrado.");

        db.MapTokens.Remove(token);
        await db.SaveChangesAsync();
    }

    // ── Annotations ───────────────────────────────────────────────────────────

    public async Task<List<AnnotationDto>> GetAnnotationsAsync(Guid sceneId)
    {
        var list = await db.SceneAnnotations
            .Where(a => a.SceneId == sceneId)
            .OrderBy(a => a.CreatedAt)
            .ToListAsync();
        return list.Select(ToAnnotationDto).ToList();
    }

    public async Task<AnnotationDto> AddAnnotationAsync(Guid sceneId, CreateAnnotationDto dto, Guid userId)
    {
        var annotation = new SceneAnnotation
        {
            Id = Guid.NewGuid(),
            SceneId = sceneId,
            Type = dto.Type,
            Points = JsonSerializer.Serialize(dto.Points),
            Color = dto.Color,
            StrokeWidth = dto.StrokeWidth,
            FillColor = dto.FillColor,
            FillOpacity = dto.FillOpacity,
            Text = dto.Text,
            FontSize = dto.FontSize,
            CreatedBy = userId,
            CreatedAt = DateTimeOffset.UtcNow
        };
        db.SceneAnnotations.Add(annotation);
        await db.SaveChangesAsync();
        return ToAnnotationDto(annotation);
    }

    public async Task DeleteAnnotationAsync(Guid annotationId)
    {
        var annotation = await db.SceneAnnotations.FindAsync(annotationId)
            ?? throw new KeyNotFoundException("Anotación no encontrada.");
        db.SceneAnnotations.Remove(annotation);
        await db.SaveChangesAsync();
    }

    public async Task ClearAnnotationsAsync(Guid sceneId)
    {
        await db.SceneAnnotations
            .Where(a => a.SceneId == sceneId)
            .ExecuteDeleteAsync();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static SceneDto ToSceneDto(SessionScene s) =>
        new(s.Id, s.SessionId, s.Name, s.BackgroundUrl, s.GridSize, s.GridEnabled, s.IsActive, s.FogEnabled);

    private static AnnotationDto ToAnnotationDto(SceneAnnotation a)
    {
        double[] pts;
        try { pts = JsonSerializer.Deserialize<double[]>(a.Points) ?? []; }
        catch { pts = []; }
        return new(a.Id, a.SceneId, a.Type, pts, a.Color, a.StrokeWidth,
                   a.FillColor, a.FillOpacity, a.Text, a.FontSize, a.CreatedBy, a.CreatedAt);
    }

    private static MapTokenDto ToTokenDto(MapToken t) =>
        new(t.Id, t.SceneId, t.EntityId, t.Label, t.ImageUrl, t.Color,
            t.X, t.Y, t.Width, t.Height, t.IsVisible, t.ControlledBy,
            t.Conditions.Select(c => new TokenConditionInfo(c.Id, c.Condition)).ToList(),
            t.Bar1Value, t.Bar1Max, t.Bar1Color,
            t.Bar2Value, t.Bar2Max, t.Bar2Color,
            t.AuraRadius, t.AuraColor, t.AuraOpacity,
            t.Rotation, t.IsLocked);
}
