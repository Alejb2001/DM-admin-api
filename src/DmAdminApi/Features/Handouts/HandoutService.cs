using DmAdminApi.Features.Handouts.Dtos;
using DmAdminApi.Infrastructure.Data;
using DmAdminApi.Infrastructure.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace DmAdminApi.Features.Handouts;

public class HandoutService(AppDbContext db)
{
    public async Task<List<HandoutDto>> GetAsync(Guid campaignId, bool isDm)
    {
        var q = db.Handouts
            .Include(h => h.Creator)
            .Where(h => h.CampaignId == campaignId);
        if (!isDm) q = q.Where(h => h.IsRevealed);
        return await q.OrderByDescending(h => h.CreatedAt)
            .Select(h => ToDto(h))
            .ToListAsync();
    }

    public async Task<HandoutDto> CreateAsync(Guid campaignId, Guid userId, CreateHandoutDto dto)
    {
        var h = new Handout
        {
            CampaignId = campaignId,
            Title = dto.Title,
            Type = dto.Type,
            Content = dto.Content,
            CreatedBy = userId,
        };
        db.Handouts.Add(h);
        await db.SaveChangesAsync();
        await db.Entry(h).Reference(x => x.Creator).LoadAsync();
        return ToDto(h);
    }

    public async Task<HandoutDto?> UpdateAsync(Guid handoutId, UpdateHandoutDto dto)
    {
        var h = await db.Handouts.Include(x => x.Creator).FirstOrDefaultAsync(x => x.Id == handoutId);
        if (h is null) return null;
        h.Title = dto.Title;
        h.Type = dto.Type;
        h.Content = dto.Content;
        await db.SaveChangesAsync();
        return ToDto(h);
    }

    public async Task<HandoutDto?> ToggleRevealAsync(Guid handoutId)
    {
        var h = await db.Handouts.Include(x => x.Creator).FirstOrDefaultAsync(x => x.Id == handoutId);
        if (h is null) return null;
        h.IsRevealed = !h.IsRevealed;
        await db.SaveChangesAsync();
        return ToDto(h);
    }

    public async Task<bool> DeleteAsync(Guid handoutId)
    {
        var h = await db.Handouts.FindAsync(handoutId);
        if (h is null) return false;
        db.Handouts.Remove(h);
        await db.SaveChangesAsync();
        return true;
    }

    private static HandoutDto ToDto(Handout h) =>
        new(h.Id, h.Title, h.Type, h.Content, h.IsRevealed,
            h.Creator?.DisplayName ?? "", h.CreatedAt);
}
