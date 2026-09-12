using DmAdminApi.Features.CharacterSheets.Dtos;
using DmAdminApi.Features.Permissions;
using DmAdminApi.Features.Sessions;
using DmAdminApi.Infrastructure.Data;
using DmAdminApi.Infrastructure.Data.Entities;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace DmAdminApi.Features.CharacterSheets;

public class CharacterSheetService(
    AppDbContext db,
    PermissionService permissions,
    IHubContext<SessionHub> hub)
{
    public async Task<List<CharacterResourceDto>> GetResourcesAsync(Guid entityId, Guid campaignId, Guid userId)
    {
        var access = await permissions.GetEntityAccessAsync(entityId, campaignId, userId);
        if (access == EntityAccess.None)
            throw new UnauthorizedAccessException("Entity not visible.");

        return await db.CharacterResources
            .Where(r => r.EntityId == entityId)
            .OrderBy(r => r.SortOrder)
            .Select(r => new CharacterResourceDto(r.Id, r.EntityId, r.Name, r.Current, r.Max, r.Color, r.SortOrder))
            .ToListAsync();
    }

    public async Task<CharacterResourceDto> CreateResourceAsync(
        Guid entityId, Guid campaignId, CreateCharacterResourceDto dto, Guid userId)
    {
        var entity = await db.WorldEntities
            .Include(e => e.EntityType)
            .Include(e => e.Permissions)
            .FirstOrDefaultAsync(e => e.Id == entityId && e.CampaignId == campaignId)
            ?? throw new KeyNotFoundException("Entity not found.");

        if (!await permissions.CanEditEntityAsync(entity, campaignId, userId))
            throw new UnauthorizedAccessException("Not allowed to edit this entity.");

        var resource = new CharacterResource
        {
            Id = Guid.NewGuid(),
            EntityId = entityId,
            Name = dto.Name,
            Current = dto.Current,
            Max = dto.Max,
            Color = dto.Color,
            SortOrder = dto.SortOrder,
            UpdatedAt = DateTimeOffset.UtcNow,
        };

        db.CharacterResources.Add(resource);
        await db.SaveChangesAsync();

        return ToDto(resource);
    }

    public async Task<CharacterResourceDto> UpdateResourceValueAsync(
        Guid entityId, Guid resourceId, Guid campaignId,
        UpdateResourceValueDto dto, Guid userId, Guid? sessionId = null)
    {
        var entity = await db.WorldEntities
            .Include(e => e.EntityType)
            .Include(e => e.Permissions)
            .FirstOrDefaultAsync(e => e.Id == entityId && e.CampaignId == campaignId)
            ?? throw new KeyNotFoundException("Entity not found.");

        if (!await permissions.CanEditEntityAsync(entity, campaignId, userId))
            throw new UnauthorizedAccessException("Not allowed to edit this entity.");

        var resource = await db.CharacterResources
            .FirstOrDefaultAsync(r => r.Id == resourceId && r.EntityId == entityId)
            ?? throw new KeyNotFoundException("Resource not found.");

        resource.Current = Math.Clamp(dto.Current, 0, resource.Max);
        resource.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync();

        var result = ToDto(resource);

        if (sessionId.HasValue)
        {
            await hub.Clients.Group(sessionId.Value.ToString())
                .SendAsync(SheetEvents.ResourceUpdated, result);
        }

        return result;
    }

    public async Task DeleteResourceAsync(
        Guid entityId, Guid resourceId, Guid campaignId, Guid userId)
    {
        var entity = await db.WorldEntities
            .Include(e => e.EntityType)
            .Include(e => e.Permissions)
            .FirstOrDefaultAsync(e => e.Id == entityId && e.CampaignId == campaignId)
            ?? throw new KeyNotFoundException("Entity not found.");

        if (!await permissions.CanEditEntityAsync(entity, campaignId, userId))
            throw new UnauthorizedAccessException("Not allowed to edit this entity.");

        var resource = await db.CharacterResources
            .FirstOrDefaultAsync(r => r.Id == resourceId && r.EntityId == entityId)
            ?? throw new KeyNotFoundException("Resource not found.");

        db.CharacterResources.Remove(resource);
        await db.SaveChangesAsync();
    }

    private static CharacterResourceDto ToDto(CharacterResource r) =>
        new(r.Id, r.EntityId, r.Name, r.Current, r.Max, r.Color, r.SortOrder);
}
