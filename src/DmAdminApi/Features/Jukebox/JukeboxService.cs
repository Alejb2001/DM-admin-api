using DmAdminApi.Features.Jukebox.Dtos;
using DmAdminApi.Infrastructure.Data;
using DmAdminApi.Infrastructure.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace DmAdminApi.Features.Jukebox;

public class JukeboxService(AppDbContext db)
{
    public async Task<List<JukeboxTrackDto>> GetAsync(Guid campaignId) =>
        await db.JukeboxTracks
            .Where(t => t.CampaignId == campaignId)
            .OrderBy(t => t.SortOrder).ThenBy(t => t.CreatedAt)
            .Select(t => new JukeboxTrackDto(t.Id, t.Name, t.Url, t.SortOrder))
            .ToListAsync();

    public async Task<JukeboxTrackDto> CreateAsync(Guid campaignId, CreateTrackDto dto)
    {
        var maxOrder = await db.JukeboxTracks
            .Where(t => t.CampaignId == campaignId)
            .Select(t => (int?)t.SortOrder).MaxAsync() ?? -1;
        var track = new JukeboxTrack
        {
            CampaignId = campaignId,
            Name = dto.Name,
            Url = dto.Url,
            SortOrder = maxOrder + 1,
        };
        db.JukeboxTracks.Add(track);
        await db.SaveChangesAsync();
        return new JukeboxTrackDto(track.Id, track.Name, track.Url, track.SortOrder);
    }

    public async Task<JukeboxTrackDto?> UpdateAsync(Guid trackId, UpdateTrackDto dto)
    {
        var t = await db.JukeboxTracks.FindAsync(trackId);
        if (t is null) return null;
        t.Name = dto.Name;
        t.Url = dto.Url;
        await db.SaveChangesAsync();
        return new JukeboxTrackDto(t.Id, t.Name, t.Url, t.SortOrder);
    }

    public async Task<bool> DeleteAsync(Guid trackId)
    {
        var t = await db.JukeboxTracks.FindAsync(trackId);
        if (t is null) return false;
        db.JukeboxTracks.Remove(t);
        await db.SaveChangesAsync();
        return true;
    }
}
