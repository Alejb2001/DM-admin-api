using DmAdminApi.Features.Maps.Dtos;
using DmAdminApi.Features.Sessions;
using DmAdminApi.Features.VttAdvanced.Dtos;
using DmAdminApi.Infrastructure.Data;
using DmAdminApi.Infrastructure.Data.Entities;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace DmAdminApi.Features.VttAdvanced;

public class TokenConditionService(AppDbContext db, IHubContext<SessionHub> hub)
{
    public async Task<List<TokenConditionInfo>> AddConditionAsync(
        Guid tokenId, Guid sessionId, AddConditionDto dto)
    {
        // Avoid duplicates for the same token
        var exists = await db.TokenConditions
            .AnyAsync(tc => tc.TokenId == tokenId && tc.Condition == dto.Condition);
        if (exists)
            throw new InvalidOperationException("El token ya tiene esta condición.");

        var condition = new TokenCondition
        {
            Id = Guid.NewGuid(),
            TokenId = tokenId,
            Condition = dto.Condition
        };
        db.TokenConditions.Add(condition);
        await db.SaveChangesAsync();

        var conditions = await GetConditionsAsync(tokenId);
        await hub.Clients.Group(sessionId.ToString())
            .SendAsync(CombatEvents.ConditionAdded, new { tokenId, conditions });

        return conditions;
    }

    public async Task<List<TokenConditionInfo>> RemoveConditionAsync(
        Guid conditionId, Guid tokenId, Guid sessionId)
    {
        var condition = await db.TokenConditions.FindAsync(conditionId)
            ?? throw new KeyNotFoundException("Condición no encontrada.");

        db.TokenConditions.Remove(condition);
        await db.SaveChangesAsync();

        var conditions = await GetConditionsAsync(tokenId);
        await hub.Clients.Group(sessionId.ToString())
            .SendAsync(CombatEvents.ConditionRemoved, new { tokenId, conditionId, conditions });

        return conditions;
    }

    private async Task<List<TokenConditionInfo>> GetConditionsAsync(Guid tokenId)
    {
        return await db.TokenConditions
            .Where(tc => tc.TokenId == tokenId)
            .Select(tc => new TokenConditionInfo(tc.Id, tc.Condition))
            .ToListAsync();
    }
}
