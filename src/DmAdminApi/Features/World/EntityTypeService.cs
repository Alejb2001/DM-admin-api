using DmAdminApi.Features.World.Dtos;
using DmAdminApi.Infrastructure.Data;
using DmAdminApi.Infrastructure.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace DmAdminApi.Features.World;

public class EntityTypeService(AppDbContext db)
{
    public async Task<List<EntityTypeWithFieldsDto>> GetTypesWithFieldsAsync(Guid campaignId)
    {
        return await db.EntityTypes
            .Include(et => et.Fields.OrderBy(f => f.SortOrder))
            .Where(et => et.CampaignId == null || et.CampaignId == campaignId)
            .OrderBy(et => et.IsSystemDefault ? 0 : 1).ThenBy(et => et.Name)
            .Select(et => new EntityTypeWithFieldsDto(
                et.Id, et.Name, et.Icon, et.Color, et.IsSystemDefault, et.CampaignId,
                et.Fields.OrderBy(f => f.SortOrder)
                    .Select(f => new EntityTypeFieldDto(f.Id, f.Name, f.FieldType, f.IsRequired, f.SortOrder))
                    .ToList()))
            .ToListAsync();
    }

    public async Task<EntityTypeWithFieldsDto> CreateTypeAsync(Guid campaignId, CreateEntityTypeDto dto)
    {
        var entityType = new EntityType
        {
            Id = Guid.NewGuid(),
            CampaignId = campaignId,
            Name = dto.Name,
            Icon = dto.Icon,
            Color = dto.Color,
            IsSystemDefault = false,
        };

        db.EntityTypes.Add(entityType);
        await db.SaveChangesAsync();

        return new EntityTypeWithFieldsDto(
            entityType.Id, entityType.Name, entityType.Icon, entityType.Color,
            entityType.IsSystemDefault, entityType.CampaignId, []);
    }

    public async Task<EntityTypeWithFieldsDto> UpdateTypeAsync(Guid typeId, Guid campaignId, UpdateEntityTypeDto dto)
    {
        var entityType = await db.EntityTypes
            .Include(et => et.Fields.OrderBy(f => f.SortOrder))
            .FirstOrDefaultAsync(et => et.Id == typeId && et.CampaignId == campaignId)
            ?? throw new KeyNotFoundException("Entity type not found or is a system type.");

        if (entityType.IsSystemDefault)
            throw new InvalidOperationException("Cannot modify system entity types.");

        entityType.Name = dto.Name;
        entityType.Icon = dto.Icon;
        entityType.Color = dto.Color;
        await db.SaveChangesAsync();

        return ToDto(entityType);
    }

    public async Task DeleteTypeAsync(Guid typeId, Guid campaignId)
    {
        var entityType = await db.EntityTypes
            .FirstOrDefaultAsync(et => et.Id == typeId && et.CampaignId == campaignId)
            ?? throw new KeyNotFoundException("Entity type not found or is a system type.");

        if (entityType.IsSystemDefault)
            throw new InvalidOperationException("Cannot delete system entity types.");

        var hasEntities = await db.WorldEntities.AnyAsync(e => e.EntityTypeId == typeId);
        if (hasEntities)
            throw new InvalidOperationException("Cannot delete an entity type that has entities. Delete the entities first.");

        db.EntityTypes.Remove(entityType);
        await db.SaveChangesAsync();
    }

    // ── Fields ────────────────────────────────────────────────────────────────

    public async Task<List<EntityTypeFieldDto>> GetFieldsAsync(Guid typeId, Guid campaignId)
    {
        var typeExists = await db.EntityTypes
            .AnyAsync(et => et.Id == typeId && (et.CampaignId == null || et.CampaignId == campaignId));

        if (!typeExists) throw new KeyNotFoundException("Entity type not found.");

        return await db.EntityTypeFields
            .Where(f => f.EntityTypeId == typeId)
            .OrderBy(f => f.SortOrder)
            .Select(f => new EntityTypeFieldDto(f.Id, f.Name, f.FieldType, f.IsRequired, f.SortOrder))
            .ToListAsync();
    }

    public async Task<EntityTypeFieldDto> AddFieldAsync(Guid typeId, Guid campaignId, CreateEntityTypeFieldDto dto)
    {
        var entityType = await db.EntityTypes
            .FirstOrDefaultAsync(et => et.Id == typeId && et.CampaignId == campaignId)
            ?? throw new KeyNotFoundException("Entity type not found or is a system type.");

        if (entityType.IsSystemDefault)
            throw new InvalidOperationException("Cannot add fields to system entity types.");

        ValidateFieldType(dto.FieldType);

        var field = new EntityTypeField
        {
            EntityTypeId = typeId,
            Name = dto.Name,
            FieldType = dto.FieldType,
            IsRequired = dto.IsRequired,
            SortOrder = dto.SortOrder,
        };

        db.EntityTypeFields.Add(field);
        await db.SaveChangesAsync();

        return new EntityTypeFieldDto(field.Id, field.Name, field.FieldType, field.IsRequired, field.SortOrder);
    }

    public async Task<EntityTypeFieldDto> UpdateFieldAsync(Guid typeId, Guid campaignId, Guid fieldId, UpdateEntityTypeFieldDto dto)
    {
        var field = await db.EntityTypeFields
            .Include(f => f.EntityType)
            .FirstOrDefaultAsync(f => f.Id == fieldId && f.EntityTypeId == typeId && f.EntityType.CampaignId == campaignId)
            ?? throw new KeyNotFoundException("Field not found.");

        ValidateFieldType(dto.FieldType);

        field.Name = dto.Name;
        field.FieldType = dto.FieldType;
        field.IsRequired = dto.IsRequired;
        field.SortOrder = dto.SortOrder;
        await db.SaveChangesAsync();

        return new EntityTypeFieldDto(field.Id, field.Name, field.FieldType, field.IsRequired, field.SortOrder);
    }

    public async Task DeleteFieldAsync(Guid typeId, Guid campaignId, Guid fieldId)
    {
        var field = await db.EntityTypeFields
            .Include(f => f.EntityType)
            .FirstOrDefaultAsync(f => f.Id == fieldId && f.EntityTypeId == typeId && f.EntityType.CampaignId == campaignId)
            ?? throw new KeyNotFoundException("Field not found.");

        db.EntityTypeFields.Remove(field);
        await db.SaveChangesAsync();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static void ValidateFieldType(string fieldType)
    {
        var valid = new[] { "text", "number", "date", "boolean", "reference", "richtext", "url" };
        if (!valid.Contains(fieldType))
            throw new ArgumentException($"Invalid field type: {fieldType}. Must be one of: {string.Join(", ", valid)}");
    }

    private static EntityTypeWithFieldsDto ToDto(EntityType et) => new(
        et.Id, et.Name, et.Icon, et.Color, et.IsSystemDefault, et.CampaignId,
        et.Fields.OrderBy(f => f.SortOrder)
            .Select(f => new EntityTypeFieldDto(f.Id, f.Name, f.FieldType, f.IsRequired, f.SortOrder))
            .ToList());
}
