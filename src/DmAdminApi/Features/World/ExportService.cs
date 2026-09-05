using DmAdminApi.Features.World.Dtos;
using DmAdminApi.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace DmAdminApi.Features.World;

public class ExportService(AppDbContext db)
{
    public async Task<CampaignExportDto?> ExportCampaignAsync(Guid campaignId)
    {
        var campaign = await db.Campaigns
            .FirstOrDefaultAsync(c => c.Id == campaignId);

        if (campaign is null) return null;

        var entityTypes = await db.EntityTypes
            .Include(et => et.Fields.OrderBy(f => f.SortOrder))
            .Where(et => et.CampaignId == null || et.CampaignId == campaignId)
            .OrderBy(et => et.IsSystemDefault ? 0 : 1).ThenBy(et => et.Name)
            .ToListAsync();

        var entities = await db.WorldEntities
            .Include(e => e.EntityType)
            .Where(e => e.CampaignId == campaignId)
            .OrderBy(e => e.Name)
            .ToListAsync();

        var relTypes = await db.RelationshipTypes
            .Where(rt => rt.CampaignId == campaignId)
            .OrderBy(rt => rt.LabelForward)
            .ToListAsync();

        var relationships = await db.EntityRelationships
            .Include(er => er.SourceEntity)
            .Include(er => er.TargetEntity)
            .Include(er => er.RelationshipType)
            .Where(er => er.SourceEntity.CampaignId == campaignId)
            .ToListAsync();

        return new CampaignExportDto(
            campaign.Id,
            campaign.Name,
            campaign.Description,
            campaign.CreatedAt,
            DateTime.UtcNow.ToString("O"),
            entityTypes.Select(et => new EntityTypeExportDto(
                et.Id, et.Name, et.Icon, et.Color, et.IsSystemDefault,
                et.Fields.Select(f => new EntityTypeFieldExportDto(f.Id, f.Name, f.FieldType, f.IsRequired, f.SortOrder)).ToList()
            )).ToList(),
            entities.Select(e => new EntityExportDto(
                e.Id, e.EntityTypeId, e.EntityType.Name, e.Name, e.Slug,
                e.CustomFields, e.CreatedAt, e.UpdatedAt
            )).ToList(),
            relTypes.Select(rt => new RelationshipTypeExportDto(
                rt.Id, rt.LabelForward, rt.LabelInverse, rt.SourceTypeId, rt.TargetTypeId
            )).ToList(),
            relationships.Select(er => new EntityRelationshipExportDto(
                er.Id, er.SourceEntityId, er.SourceEntity.Name,
                er.TargetEntityId, er.TargetEntity.Name,
                er.RelationshipTypeId, er.RelationshipType.LabelForward, er.Notes
            )).ToList()
        );
    }

    public async Task<List<EntityChangeLogDto>> GetEntityHistoryAsync(Guid entityId)
    {
        return await db.EntityChangeLogs
            .Where(cl => cl.EntityId == entityId)
            .OrderByDescending(cl => cl.ChangedAt)
            .Take(100)
            .Select(cl => new EntityChangeLogDto(
                cl.Id, cl.UserId, cl.UserDisplayName, cl.ChangedAt,
                cl.FieldChanged, cl.OldValue, cl.NewValue))
            .ToListAsync();
    }
}
