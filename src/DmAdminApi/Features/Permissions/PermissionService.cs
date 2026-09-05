using DmAdminApi.Infrastructure.Data;
using DmAdminApi.Infrastructure.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace DmAdminApi.Features.Permissions;

public class PermissionService(AppDbContext db)
{
    /// <summary>Returns true if userId is the DM owner of the campaign.</summary>
    public async Task<bool> IsCampaignOwnerAsync(Guid campaignId, Guid userId)
    {
        return await db.Campaigns.AnyAsync(c => c.Id == campaignId && c.OwnerId == userId);
    }

    /// <summary>Returns the campaign member record for a user, or null.</summary>
    public async Task<CampaignMember?> GetMemberAsync(Guid campaignId, Guid userId)
    {
        return await db.CampaignMembers
            .Include(m => m.Role)
            .FirstOrDefaultAsync(m => m.CampaignId == campaignId && m.UserId == userId);
    }

    /// <summary>
    /// Returns the access level for a user on a specific entity.
    /// DM owner and Co-DM get full access without consulting entity_permissions.
    /// </summary>
    public async Task<EntityAccess> GetEntityAccessAsync(Guid entityId, Guid campaignId, Guid userId)
    {
        if (await IsCampaignOwnerAsync(campaignId, userId))
            return EntityAccess.FullAccess;

        var member = await GetMemberAsync(campaignId, userId);
        if (member is null) return EntityAccess.None;

        if (member.Role.Name == SystemRoles.CoDm)
            return EntityAccess.FullAccess;

        var permission = await db.EntityPermissions
            .FirstOrDefaultAsync(ep => ep.EntityId == entityId && ep.RoleId == member.RoleId);

        if (permission is null || !permission.CanView)
            return EntityAccess.None;

        return permission.CanEdit ? EntityAccess.FullAccess : EntityAccess.ReadOnly;
    }

    /// <summary>
    /// Filters a query to only include entities visible to the user.
    /// DM owner / Co-DM see all. Others see only entities with can_view = true for their role.
    /// </summary>
    public async Task<IQueryable<WorldEntity>> FilterVisibleEntitiesAsync(
        IQueryable<WorldEntity> query, Guid campaignId, Guid userId)
    {
        if (await IsCampaignOwnerAsync(campaignId, userId))
            return query;

        var member = await GetMemberAsync(campaignId, userId);
        if (member is null) return query.Where(_ => false);

        if (member.Role.Name == SystemRoles.CoDm) return query;

        var roleId = member.RoleId;
        return query.Where(e =>
            e.Permissions.Any(p => p.RoleId == roleId && p.CanView));
    }

    /// <summary>Returns true if user can edit a specific entity (owner/co-dm always can; player can edit own character).</summary>
    public async Task<bool> CanEditEntityAsync(WorldEntity entity, Guid campaignId, Guid userId)
    {
        if (await IsCampaignOwnerAsync(campaignId, userId)) return true;

        var member = await GetMemberAsync(campaignId, userId);
        if (member is null) return false;

        if (member.Role.Name == SystemRoles.CoDm) return true;

        // Player can edit their own Character
        if (entity.EntityType.Name == "Personaje" && entity.CreatedBy == userId)
        {
            var permission = await db.EntityPermissions
                .FirstOrDefaultAsync(ep => ep.EntityId == entity.Id && ep.RoleId == member.RoleId);
            return permission?.CanView ?? false;
        }

        var perm = await db.EntityPermissions
            .FirstOrDefaultAsync(ep => ep.EntityId == entity.Id && ep.RoleId == member.RoleId);
        return perm?.CanEdit ?? false;
    }
}

public enum EntityAccess { None, ReadOnly, FullAccess }
