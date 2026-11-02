using DmAdminApi.Common.Controllers;
using DmAdminApi.Features.Jukebox.Dtos;
using DmAdminApi.Features.Sessions;
using DmAdminApi.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace DmAdminApi.Features.Jukebox;

[Route("api/campaigns/{campaignId:guid}/tracks")]
public class JukeboxController(JukeboxService svc, AppDbContext db,
    IHubContext<SessionHub> hub) : ApiControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(Guid campaignId) =>
        Ok(await svc.GetAsync(campaignId));

    [HttpPost]
    public async Task<IActionResult> Create(Guid campaignId, [FromBody] CreateTrackDto dto)
    {
        if (!await IsDm(campaignId)) return Forbid();
        return StatusCode(201, await svc.CreateAsync(campaignId, dto));
    }

    [HttpPut("{trackId:guid}")]
    public async Task<IActionResult> Update(Guid campaignId, Guid trackId, [FromBody] UpdateTrackDto dto)
    {
        if (!await IsDm(campaignId)) return Forbid();
        var r = await svc.UpdateAsync(trackId, dto);
        return r is null ? NotFound() : Ok(r);
    }

    [HttpDelete("{trackId:guid}")]
    public async Task<IActionResult> Delete(Guid campaignId, Guid trackId)
    {
        if (!await IsDm(campaignId)) return Forbid();
        return await svc.DeleteAsync(trackId) ? NoContent() : NotFound();
    }

    // ── Playback control (broadcast via SignalR) ───────────────────────────────

    [HttpPost("{sessionId:guid}/play")]
    public async Task<IActionResult> Play(Guid campaignId, Guid sessionId, [FromBody] PlayTrackDto dto)
    {
        if (!await IsDm(campaignId)) return Forbid();
        await hub.Clients.Group(sessionId.ToString())
            .SendAsync(JukeboxEvents.TrackPlay, dto);
        return NoContent();
    }

    [HttpPost("{sessionId:guid}/stop")]
    public async Task<IActionResult> Stop(Guid campaignId, Guid sessionId)
    {
        if (!await IsDm(campaignId)) return Forbid();
        await hub.Clients.Group(sessionId.ToString())
            .SendAsync(JukeboxEvents.TrackStop);
        return NoContent();
    }

    private async Task<bool> IsDm(Guid campaignId) =>
        await db.Campaigns.AnyAsync(c => c.Id == campaignId && c.OwnerId == CurrentUserId);
}

public static class JukeboxEvents
{
    public const string TrackPlay = "TrackPlay";
    public const string TrackStop = "TrackStop";
}
