using DmAdminApi.Features.Tables.Dtos;
using DmAdminApi.Infrastructure.Data;
using DmAdminApi.Infrastructure.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace DmAdminApi.Features.Tables;

public class TableService(AppDbContext db)
{
    public async Task<List<RollableTableDto>> GetAllAsync(Guid campaignId) =>
        await db.RollableTables
            .Include(t => t.Creator)
            .Include(t => t.Entries)
            .Where(t => t.CampaignId == campaignId)
            .OrderBy(t => t.Name)
            .Select(t => ToDto(t))
            .ToListAsync();

    public async Task<RollableTableDto?> GetAsync(Guid tableId) =>
        await db.RollableTables
            .Include(t => t.Creator)
            .Include(t => t.Entries)
            .Where(t => t.Id == tableId)
            .Select(t => ToDto(t))
            .FirstOrDefaultAsync();

    public async Task<RollableTableDto> CreateAsync(Guid campaignId, Guid userId, CreateTableDto dto)
    {
        var table = new RollableTable
        {
            CampaignId = campaignId,
            Name = dto.Name,
            Description = dto.Description,
            CreatedBy = userId,
        };
        db.RollableTables.Add(table);
        await db.SaveChangesAsync();
        await db.Entry(table).Reference(t => t.Creator).LoadAsync();
        return ToDto(table);
    }

    public async Task<RollableTableDto?> UpdateAsync(Guid tableId, UpdateTableDto dto)
    {
        var t = await db.RollableTables.Include(x => x.Creator).Include(x => x.Entries)
                    .FirstOrDefaultAsync(x => x.Id == tableId);
        if (t is null) return null;
        t.Name = dto.Name;
        t.Description = dto.Description;
        await db.SaveChangesAsync();
        return ToDto(t);
    }

    public async Task<bool> DeleteAsync(Guid tableId)
    {
        var t = await db.RollableTables.FindAsync(tableId);
        if (t is null) return false;
        db.RollableTables.Remove(t);
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<TableEntryDto> AddEntryAsync(Guid tableId, CreateEntryDto dto)
    {
        var entry = new TableEntry { TableId = tableId, Text = dto.Text, Weight = dto.Weight };
        db.TableEntries.Add(entry);
        await db.SaveChangesAsync();
        return new TableEntryDto(entry.Id, entry.Text, entry.Weight);
    }

    public async Task<TableEntryDto?> UpdateEntryAsync(Guid entryId, UpdateEntryDto dto)
    {
        var e = await db.TableEntries.FindAsync(entryId);
        if (e is null) return null;
        e.Text = dto.Text;
        e.Weight = dto.Weight;
        await db.SaveChangesAsync();
        return new TableEntryDto(e.Id, e.Text, e.Weight);
    }

    public async Task<bool> DeleteEntryAsync(Guid entryId)
    {
        var e = await db.TableEntries.FindAsync(entryId);
        if (e is null) return false;
        db.TableEntries.Remove(e);
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<TableRollResultDto?> RollAsync(Guid tableId)
    {
        var table = await db.RollableTables
            .Include(t => t.Entries)
            .FirstOrDefaultAsync(t => t.Id == tableId);
        if (table is null || table.Entries.Count == 0) return null;

        var totalWeight = table.Entries.Sum(e => e.Weight);
        var roll = Random.Shared.Next(1, totalWeight + 1);
        var cumulative = 0;
        TableEntry? picked = null;
        foreach (var entry in table.Entries)
        {
            cumulative += entry.Weight;
            if (roll <= cumulative) { picked = entry; break; }
        }
        picked ??= table.Entries.Last();
        return new TableRollResultDto(table.Id, table.Name,
            new TableEntryDto(picked.Id, picked.Text, picked.Weight));
    }

    private static RollableTableDto ToDto(RollableTable t) =>
        new(t.Id, t.Name, t.Description, t.Creator?.DisplayName ?? "",
            t.CreatedAt, t.Entries.Select(e => new TableEntryDto(e.Id, e.Text, e.Weight)).ToList());
}
