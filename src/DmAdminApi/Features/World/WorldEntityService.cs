using System.Text.Json;
using DmAdminApi.Features.Hubs;
using DmAdminApi.Features.Permissions;
using DmAdminApi.Features.World.Dtos;
using DmAdminApi.Infrastructure.Data;
using DmAdminApi.Infrastructure.Data.Entities;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace DmAdminApi.Features.World;

public class WorldEntityService(
    AppDbContext db,
    PermissionService permissions,
    IHubContext<CampaignHub> hub)
{
    public async Task<List<EntityTypeDto>> GetEntityTypesAsync(Guid campaignId)
    {
        return await db.EntityTypes
            .Where(et => et.CampaignId == null || et.CampaignId == campaignId)
            .Select(et => new EntityTypeDto(et.Id, et.Name, et.Icon, et.Color, et.IsSystemDefault, et.CampaignId))
            .ToListAsync();
    }

    public async Task<List<WorldEntitySummaryDto>> GetEntitiesAsync(Guid campaignId, Guid userId)
    {
        var query = db.WorldEntities
            .Include(e => e.EntityType)
            .Include(e => e.Permissions).ThenInclude(p => p.Role)
            .Where(e => e.CampaignId == campaignId);

        query = await permissions.FilterVisibleEntitiesAsync(query, campaignId, userId);

        return await query
            .OrderBy(e => e.Name)
            .Select(e => new WorldEntitySummaryDto(
                e.Id, e.EntityTypeId, e.EntityType.Name, e.EntityType.Icon, e.EntityType.Color,
                e.Name, e.Slug, e.UpdatedAt))
            .ToListAsync();
    }

    public async Task<WorldEntityDto> GetEntityAsync(Guid entityId, Guid campaignId, Guid userId)
    {
        var entity = await db.WorldEntities
            .Include(e => e.EntityType).ThenInclude(et => et.Fields.OrderBy(f => f.SortOrder))
            .Include(e => e.Permissions).ThenInclude(p => p.Role)
            .FirstOrDefaultAsync(e => e.Id == entityId && e.CampaignId == campaignId)
            ?? throw new KeyNotFoundException("Entity not found.");

        var access = await permissions.GetEntityAccessAsync(entityId, campaignId, userId);
        if (access == EntityAccess.None)
            throw new UnauthorizedAccessException("Entity not visible.");

        return ToDto(entity);
    }

    public async Task<WorldEntityDto> CreateAsync(Guid campaignId, CreateWorldEntityDto dto, Guid userId)
    {
        var entityType = await db.EntityTypes.FindAsync(dto.EntityTypeId)
            ?? throw new KeyNotFoundException("Entity type not found.");

        var slug = await GenerateUniqueSlugAsync(campaignId, dto.Name);

        var entity = new WorldEntity
        {
            CampaignId = campaignId,
            EntityTypeId = dto.EntityTypeId,
            Name = dto.Name,
            Slug = slug,
            CreatedBy = userId,
            CustomFields = dto.CustomFields,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };

        db.WorldEntities.Add(entity);
        await db.SaveChangesAsync();

        var result = await GetEntityAsync(entity.Id, campaignId, userId);

        // Broadcast to campaign — summary only (entity is private, DM sees immediately)
        var summary = new WorldEntitySummaryDto(
            result.Id, result.EntityTypeId, result.EntityTypeName, result.EntityTypeIcon,
            result.EntityTypeColor, result.Name, result.Slug, result.UpdatedAt);

        await hub.Clients.Group(HubGroups.Campaign(campaignId))
            .SendAsync(HubEvents.EntityCreated, summary);

        return result;
    }

    public async Task<WorldEntityDto> UpdateAsync(Guid entityId, Guid campaignId, UpdateWorldEntityDto dto, Guid userId)
    {
        var entity = await db.WorldEntities
            .Include(e => e.EntityType).ThenInclude(et => et.Fields.OrderBy(f => f.SortOrder))
            .Include(e => e.Permissions).ThenInclude(p => p.Role)
            .FirstOrDefaultAsync(e => e.Id == entityId && e.CampaignId == campaignId)
            ?? throw new KeyNotFoundException("Entity not found.");

        if (!await permissions.CanEditEntityAsync(entity, campaignId, userId))
            throw new UnauthorizedAccessException("Not allowed to edit this entity.");

        // Optimistic concurrency check
        if (entity.UpdatedAt > dto.UpdatedAt)
            throw new InvalidOperationException("conflict");

        // Log changes before applying them
        var user = await db.Users.FindAsync(userId);
        var displayName = user?.DisplayName ?? "Usuario";

        if (entity.Name != dto.Name)
        {
            db.EntityChangeLogs.Add(new EntityChangeLog
            {
                EntityId = entityId, UserId = userId, UserDisplayName = displayName,
                ChangedAt = DateTime.UtcNow, FieldChanged = "name",
                OldValue = entity.Name, NewValue = dto.Name,
            });
        }

        var oldFieldsJson = entity.CustomFields is not null
            ? JsonSerializer.Serialize(entity.CustomFields) : null;
        var newFieldsJson = dto.CustomFields is not null
            ? JsonSerializer.Serialize(dto.CustomFields) : null;

        if (oldFieldsJson != newFieldsJson)
        {
            db.EntityChangeLogs.Add(new EntityChangeLog
            {
                EntityId = entityId, UserId = userId, UserDisplayName = displayName,
                ChangedAt = DateTime.UtcNow, FieldChanged = "customFields",
                OldValue = oldFieldsJson, NewValue = newFieldsJson,
            });
        }

        entity.Name = dto.Name;
        entity.CustomFields = dto.CustomFields;
        entity.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync();
        var result = ToDto(entity);

        // Broadcast to all members of the campaign
        await hub.Clients.Group(HubGroups.Campaign(campaignId))
            .SendAsync(HubEvents.EntityUpdated, result);

        return result;
    }

    public async Task DeleteAsync(Guid entityId, Guid campaignId)
    {
        var entity = await db.WorldEntities
            .FirstOrDefaultAsync(e => e.Id == entityId && e.CampaignId == campaignId)
            ?? throw new KeyNotFoundException("Entity not found.");

        db.WorldEntities.Remove(entity);
        await db.SaveChangesAsync();

        // Broadcast deletion to all in campaign
        await hub.Clients.Group(HubGroups.Campaign(campaignId))
            .SendAsync(HubEvents.EntityDeleted, entityId);
    }

    public async Task<List<PermissionDto>> SetPermissionAsync(Guid entityId, Guid campaignId, SetPermissionDto dto)
    {
        var existing = await db.EntityPermissions
            .FirstOrDefaultAsync(ep => ep.EntityId == entityId && ep.RoleId == dto.RoleId);

        if (existing is null)
        {
            db.EntityPermissions.Add(new EntityPermission
            {
                EntityId = entityId,
                RoleId = dto.RoleId,
                CanView = dto.CanView,
                CanEdit = dto.CanEdit,
            });
        }
        else
        {
            existing.CanView = dto.CanView;
            existing.CanEdit = dto.CanEdit;
        }

        await db.SaveChangesAsync();
        var updatedPerms = await GetPermissionsAsync(entityId);

        // Broadcast permission change to all in campaign
        // Players will use this to add/remove entities from their view
        await hub.Clients.Group(HubGroups.Campaign(campaignId))
            .SendAsync(HubEvents.PermissionsChanged, entityId, updatedPerms);

        return updatedPerms;
    }

    public async Task<List<PermissionDto>> GetPermissionsAsync(Guid entityId)
    {
        return await db.EntityPermissions
            .Include(ep => ep.Role)
            .Where(ep => ep.EntityId == entityId)
            .Select(ep => new PermissionDto(ep.RoleId, ep.Role.Name, ep.CanView, ep.CanEdit))
            .ToListAsync();
    }

    public async Task<List<EntitySearchResultDto>> SearchAsync(Guid campaignId, Guid userId, string query)
    {
        var q = db.WorldEntities
            .Include(e => e.EntityType)
            .Include(e => e.Permissions).ThenInclude(p => p.Role)
            .Where(e => e.CampaignId == campaignId && e.Name.ToLower().Contains(query.ToLower()));

        q = await permissions.FilterVisibleEntitiesAsync(q, campaignId, userId);

        return await q
            .OrderBy(e => e.Name)
            .Take(50)
            .Select(e => new EntitySearchResultDto(
                e.Id, e.EntityTypeId, e.EntityType.Name, e.EntityType.Icon, e.EntityType.Color,
                e.Name, e.Slug, e.UpdatedAt))
            .ToListAsync();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private async Task<string> GenerateUniqueSlugAsync(Guid campaignId, string name)
    {
        var baseSlug = name.ToLowerInvariant()
            .Replace(" ", "-")
            .Replace("á", "a").Replace("é", "e").Replace("í", "i")
            .Replace("ó", "o").Replace("ú", "u").Replace("ñ", "n")
            .Replace("ü", "u")
            .Where(c => char.IsLetterOrDigit(c) || c == '-')
            .Aggregate("", (s, c) => s + c);

        var slug = baseSlug;
        var counter = 1;
        while (await db.WorldEntities.AnyAsync(e => e.CampaignId == campaignId && e.Slug == slug))
            slug = $"{baseSlug}-{counter++}";

        return slug;
    }

    private static WorldEntityDto ToDto(WorldEntity e) => new(
        e.Id, e.CampaignId, e.EntityTypeId, e.EntityType.Name, e.EntityType.Icon, e.EntityType.Color,
        e.EntityType.Fields.Select(f => new EntityTypeFieldDto(f.Id, f.Name, f.FieldType, f.IsRequired, f.SortOrder)).ToList(),
        e.Name, e.Slug, e.CreatedBy, e.CustomFields, e.CreatedAt, e.UpdatedAt,
        e.Permissions.Select(p => new PermissionDto(p.RoleId, p.Role.Name, p.CanView, p.CanEdit)).ToList()
    );
}
