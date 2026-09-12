using DmAdminApi.Features.Sessions;
using DmAdminApi.Features.VttAdvanced.Dtos;
using DmAdminApi.Infrastructure.Data;
using DmAdminApi.Infrastructure.Data.Entities;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace DmAdminApi.Features.VttAdvanced;

public class FogService(AppDbContext db, IHubContext<SessionHub> hub)
{
    public async Task<List<FogZoneDto>> GetZonesAsync(Guid sceneId)
    {
        return await db.FogRevealedZones
            .Where(f => f.SceneId == sceneId)
            .OrderBy(f => f.CreatedAt)
            .Select(f => ToDto(f))
            .ToListAsync();
    }

    public async Task<FogZoneDto> AddZoneAsync(Guid sceneId, Guid sessionId, CreateFogZoneDto dto)
    {
        var zone = new FogRevealedZone
        {
            Id = Guid.NewGuid(),
            SceneId = sceneId,
            Shape = dto.Shape,
            X = dto.X,
            Y = dto.Y,
            Width = dto.Width,
            Height = dto.Height,
            Radius = dto.Radius,
            CreatedAt = DateTimeOffset.UtcNow
        };
        db.FogRevealedZones.Add(zone);
        await db.SaveChangesAsync();

        var zoneDto = ToDto(zone);
        await hub.Clients.Group(sessionId.ToString())
            .SendAsync(FogEvents.FogZoneAdded, zoneDto);

        return zoneDto;
    }

    public async Task RemoveZoneAsync(Guid zoneId, Guid sessionId)
    {
        var zone = await db.FogRevealedZones.FindAsync(zoneId)
            ?? throw new KeyNotFoundException("Zona no encontrada.");

        db.FogRevealedZones.Remove(zone);
        await db.SaveChangesAsync();

        await hub.Clients.Group(sessionId.ToString())
            .SendAsync(FogEvents.FogZoneRemoved, zoneId);
    }

    public async Task ClearFogAsync(Guid sceneId, Guid sessionId)
    {
        await db.FogRevealedZones
            .Where(f => f.SceneId == sceneId)
            .ExecuteDeleteAsync();

        await hub.Clients.Group(sessionId.ToString())
            .SendAsync(FogEvents.FogCleared, sceneId);
    }

    public async Task ToggleFogAsync(Guid sceneId, Guid sessionId, bool fogEnabled)
    {
        var scene = await db.SessionScenes.FindAsync(sceneId)
            ?? throw new KeyNotFoundException("Escena no encontrada.");

        scene.FogEnabled = fogEnabled;
        await db.SaveChangesAsync();

        await hub.Clients.Group(sessionId.ToString())
            .SendAsync(FogEvents.FogToggled, new { sceneId, fogEnabled });
    }

    private static FogZoneDto ToDto(FogRevealedZone f) =>
        new(f.Id, f.SceneId, f.Shape, f.X, f.Y, f.Width, f.Height, f.Radius);
}
