using DmAdminApi.Features.World.Dtos;
using DmAdminApi.Infrastructure.Data;
using DmAdminApi.Infrastructure.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace DmAdminApi.Features.World;

public class RelationshipService(AppDbContext db)
{
    // ── Relationship Types ────────────────────────────────────────────────────

    public async Task<List<RelationshipTypeDto>> GetRelationshipTypesAsync(Guid campaignId)
    {
        return await db.RelationshipTypes
            .Where(rt => rt.CampaignId == campaignId)
            .OrderBy(rt => rt.LabelForward)
            .Select(rt => new RelationshipTypeDto(rt.Id, rt.CampaignId, rt.LabelForward, rt.LabelInverse, rt.SourceTypeId, rt.TargetTypeId))
            .ToListAsync();
    }

    public async Task<RelationshipTypeDto> CreateRelationshipTypeAsync(Guid campaignId, CreateRelationshipTypeDto dto)
    {
        var relType = new RelationshipType
        {
            Id = Guid.NewGuid(),
            CampaignId = campaignId,
            LabelForward = dto.LabelForward,
            LabelInverse = dto.LabelInverse,
            SourceTypeId = dto.SourceTypeId,
            TargetTypeId = dto.TargetTypeId,
        };

        db.RelationshipTypes.Add(relType);
        await db.SaveChangesAsync();

        return new RelationshipTypeDto(relType.Id, relType.CampaignId, relType.LabelForward, relType.LabelInverse, relType.SourceTypeId, relType.TargetTypeId);
    }

    public async Task<RelationshipTypeDto> UpdateRelationshipTypeAsync(Guid typeId, Guid campaignId, UpdateRelationshipTypeDto dto)
    {
        var relType = await db.RelationshipTypes
            .FirstOrDefaultAsync(rt => rt.Id == typeId && rt.CampaignId == campaignId)
            ?? throw new KeyNotFoundException("Relationship type not found.");

        relType.LabelForward = dto.LabelForward;
        relType.LabelInverse = dto.LabelInverse;
        relType.SourceTypeId = dto.SourceTypeId;
        relType.TargetTypeId = dto.TargetTypeId;
        await db.SaveChangesAsync();

        return new RelationshipTypeDto(relType.Id, relType.CampaignId, relType.LabelForward, relType.LabelInverse, relType.SourceTypeId, relType.TargetTypeId);
    }

    public async Task DeleteRelationshipTypeAsync(Guid typeId, Guid campaignId)
    {
        var relType = await db.RelationshipTypes
            .FirstOrDefaultAsync(rt => rt.Id == typeId && rt.CampaignId == campaignId)
            ?? throw new KeyNotFoundException("Relationship type not found.");

        db.RelationshipTypes.Remove(relType);
        await db.SaveChangesAsync();
    }

    // ── Entity Relationships ──────────────────────────────────────────────────

    public async Task<List<EntityRelationshipDto>> GetEntityRelationshipsAsync(Guid entityId)
    {
        var outgoing = await db.EntityRelationships
            .Include(er => er.SourceEntity).ThenInclude(e => e.EntityType)
            .Include(er => er.TargetEntity).ThenInclude(e => e.EntityType)
            .Include(er => er.RelationshipType)
            .Where(er => er.SourceEntityId == entityId)
            .Select(er => new EntityRelationshipDto(
                er.Id, er.SourceEntityId, er.SourceEntity.Name,
                er.TargetEntityId, er.TargetEntity.Name,
                er.TargetEntity.EntityType.Name, er.TargetEntity.EntityType.Icon, er.TargetEntity.EntityType.Color,
                er.RelationshipTypeId, er.RelationshipType.LabelForward, er.RelationshipType.LabelInverse,
                er.Notes))
            .ToListAsync();

        var incoming = await db.EntityRelationships
            .Include(er => er.SourceEntity).ThenInclude(e => e.EntityType)
            .Include(er => er.TargetEntity).ThenInclude(e => e.EntityType)
            .Include(er => er.RelationshipType)
            .Where(er => er.TargetEntityId == entityId)
            .Select(er => new EntityRelationshipDto(
                er.Id, er.TargetEntityId, er.TargetEntity.Name,
                er.SourceEntityId, er.SourceEntity.Name,
                er.SourceEntity.EntityType.Name, er.SourceEntity.EntityType.Icon, er.SourceEntity.EntityType.Color,
                er.RelationshipTypeId, er.RelationshipType.LabelInverse, er.RelationshipType.LabelForward,
                er.Notes))
            .ToListAsync();

        return [.. outgoing, .. incoming];
    }

    public async Task<EntityRelationshipDto> CreateEntityRelationshipAsync(Guid sourceEntityId, Guid campaignId, CreateEntityRelationshipDto dto)
    {
        var sourceEntity = await db.WorldEntities
            .FirstOrDefaultAsync(e => e.Id == sourceEntityId && e.CampaignId == campaignId)
            ?? throw new KeyNotFoundException("Source entity not found.");

        var targetEntity = await db.WorldEntities
            .Include(e => e.EntityType)
            .FirstOrDefaultAsync(e => e.Id == dto.TargetEntityId && e.CampaignId == campaignId)
            ?? throw new KeyNotFoundException("Target entity not found.");

        var relType = await db.RelationshipTypes
            .FirstOrDefaultAsync(rt => rt.Id == dto.RelationshipTypeId && rt.CampaignId == campaignId)
            ?? throw new KeyNotFoundException("Relationship type not found.");

        // Prevent duplicate relationships
        var exists = await db.EntityRelationships.AnyAsync(er =>
            er.SourceEntityId == sourceEntityId &&
            er.TargetEntityId == dto.TargetEntityId &&
            er.RelationshipTypeId == dto.RelationshipTypeId);

        if (exists) throw new InvalidOperationException("This relationship already exists.");

        var relationship = new EntityRelationship
        {
            Id = Guid.NewGuid(),
            SourceEntityId = sourceEntityId,
            TargetEntityId = dto.TargetEntityId,
            RelationshipTypeId = dto.RelationshipTypeId,
            Notes = dto.Notes,
        };

        db.EntityRelationships.Add(relationship);
        await db.SaveChangesAsync();

        return new EntityRelationshipDto(
            relationship.Id, sourceEntityId, sourceEntity.Name,
            targetEntity.Id, targetEntity.Name,
            targetEntity.EntityType.Name, targetEntity.EntityType.Icon, targetEntity.EntityType.Color,
            relType.Id, relType.LabelForward, relType.LabelInverse,
            relationship.Notes);
    }

    public async Task DeleteEntityRelationshipAsync(Guid relationshipId, Guid campaignId)
    {
        var rel = await db.EntityRelationships
            .Include(er => er.SourceEntity)
            .FirstOrDefaultAsync(er => er.Id == relationshipId && er.SourceEntity.CampaignId == campaignId)
            ?? throw new KeyNotFoundException("Relationship not found.");

        db.EntityRelationships.Remove(rel);
        await db.SaveChangesAsync();
    }

    // ── Graph ─────────────────────────────────────────────────────────────────

    public async Task<GraphDto> GetGraphAsync(Guid campaignId, IQueryable<WorldEntity> visibleEntities)
    {
        var entities = await visibleEntities
            .Include(e => e.EntityType)
            .Select(e => new GraphNodeDto(e.Id, e.Name, e.EntityType.Name, e.EntityType.Icon, e.EntityType.Color))
            .ToListAsync();

        var entityIds = entities.Select(e => e.Id).ToHashSet();

        var edges = await db.EntityRelationships
            .Include(er => er.RelationshipType)
            .Where(er => entityIds.Contains(er.SourceEntityId) && entityIds.Contains(er.TargetEntityId))
            .Select(er => new GraphEdgeDto(er.Id, er.SourceEntityId, er.TargetEntityId, er.RelationshipType.LabelForward))
            .ToListAsync();

        return new GraphDto(entities, edges);
    }
}
