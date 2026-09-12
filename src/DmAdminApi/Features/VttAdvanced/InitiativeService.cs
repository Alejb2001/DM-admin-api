using DmAdminApi.Features.Sessions;
using DmAdminApi.Features.VttAdvanced.Dtos;
using DmAdminApi.Infrastructure.Data;
using DmAdminApi.Infrastructure.Data.Entities;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace DmAdminApi.Features.VttAdvanced;

public class InitiativeService(AppDbContext db, IHubContext<SessionHub> hub)
{
    public async Task<List<InitiativeEntryDto>> GetEntriesAsync(Guid sessionId)
    {
        return await db.InitiativeEntries
            .Where(i => i.SessionId == sessionId)
            .OrderBy(i => i.SortOrder)
            .Select(i => ToDto(i))
            .ToListAsync();
    }

    public async Task<List<InitiativeEntryDto>> AddEntryAsync(Guid sessionId, CreateInitiativeEntryDto dto)
    {
        var count = await db.InitiativeEntries.CountAsync(i => i.SessionId == sessionId);
        var entry = new InitiativeEntry
        {
            Id = Guid.NewGuid(),
            SessionId = sessionId,
            TokenId = dto.TokenId,
            Name = dto.Name,
            Initiative = dto.Initiative,
            SortOrder = count,
            IsActive = false,
            CreatedAt = DateTimeOffset.UtcNow
        };
        db.InitiativeEntries.Add(entry);
        await db.SaveChangesAsync();

        var list = await GetEntriesAsync(sessionId);
        await hub.Clients.Group(sessionId.ToString())
            .SendAsync(CombatEvents.InitiativeUpdated, list);

        return list;
    }

    public async Task<List<InitiativeEntryDto>> ReorderAsync(Guid sessionId, UpdateInitiativeOrderDto dto)
    {
        var entries = await db.InitiativeEntries
            .Where(i => i.SessionId == sessionId)
            .ToListAsync();

        for (int i = 0; i < dto.OrderedIds.Count; i++)
        {
            var entry = entries.FirstOrDefault(e => e.Id == dto.OrderedIds[i]);
            if (entry is not null)
                entry.SortOrder = i;
        }
        await db.SaveChangesAsync();

        var list = await GetEntriesAsync(sessionId);
        await hub.Clients.Group(sessionId.ToString())
            .SendAsync(CombatEvents.InitiativeUpdated, list);

        return list;
    }

    public async Task<List<InitiativeEntryDto>> SetActiveAsync(Guid sessionId, Guid entryId)
    {
        await db.InitiativeEntries
            .Where(i => i.SessionId == sessionId)
            .ExecuteUpdateAsync(s => s.SetProperty(i => i.IsActive, false));

        var entry = await db.InitiativeEntries.FindAsync(entryId)
            ?? throw new KeyNotFoundException("Entrada no encontrada.");

        entry.IsActive = true;
        await db.SaveChangesAsync();

        var list = await GetEntriesAsync(sessionId);
        await hub.Clients.Group(sessionId.ToString())
            .SendAsync(CombatEvents.InitiativeTurnChanged, list);

        return list;
    }

    public async Task<List<InitiativeEntryDto>> RemoveEntryAsync(Guid sessionId, Guid entryId)
    {
        var entry = await db.InitiativeEntries.FindAsync(entryId)
            ?? throw new KeyNotFoundException("Entrada no encontrada.");

        db.InitiativeEntries.Remove(entry);
        await db.SaveChangesAsync();

        // Resequence
        var remaining = await db.InitiativeEntries
            .Where(i => i.SessionId == sessionId)
            .OrderBy(i => i.SortOrder)
            .ToListAsync();
        for (int i = 0; i < remaining.Count; i++)
            remaining[i].SortOrder = i;
        await db.SaveChangesAsync();

        var list = await GetEntriesAsync(sessionId);
        await hub.Clients.Group(sessionId.ToString())
            .SendAsync(CombatEvents.InitiativeUpdated, list);

        return list;
    }

    public async Task ClearAsync(Guid sessionId)
    {
        await db.InitiativeEntries
            .Where(i => i.SessionId == sessionId)
            .ExecuteDeleteAsync();

        await hub.Clients.Group(sessionId.ToString())
            .SendAsync(CombatEvents.InitiativeUpdated, new List<InitiativeEntryDto>());
    }

    private static InitiativeEntryDto ToDto(InitiativeEntry i) =>
        new(i.Id, i.SessionId, i.TokenId, i.Name, i.Initiative, i.SortOrder, i.IsActive);
}
