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
        var token = await db.MapTokens.FindAsync(tokenId)
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
        var token = await db.MapTokens.FindAsync(tokenId)
            ?? throw new KeyNotFoundException("Token no encontrado.");

        token.Label = dto.Label;
        token.ImageUrl = dto.ImageUrl;
        token.Color = dto.Color;
        token.Width = dto.Width > 0 ? dto.Width : 1;
        token.Height = dto.Height > 0 ? dto.Height : 1;
        token.IsVisible = dto.IsVisible;
        token.ControlledBy = dto.ControlledBy;
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

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static SceneDto ToSceneDto(SessionScene s) =>
        new(s.Id, s.SessionId, s.Name, s.BackgroundUrl, s.GridSize, s.GridEnabled, s.IsActive, s.FogEnabled);

    private static MapTokenDto ToTokenDto(MapToken t) =>
        new(t.Id, t.SceneId, t.EntityId, t.Label, t.ImageUrl, t.Color,
            t.X, t.Y, t.Width, t.Height, t.IsVisible, t.ControlledBy,
            t.Conditions.Select(c => new TokenConditionInfo(c.Id, c.Condition)).ToList());
}
