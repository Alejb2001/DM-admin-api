using DmAdminApi.Common.Controllers;
using DmAdminApi.Features.Handouts.Dtos;
using DmAdminApi.Features.Sessions;
using DmAdminApi.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace DmAdminApi.Features.Handouts;

[Route("api/campaigns/{campaignId:guid}/handouts")]
public class HandoutController(HandoutService svc, AppDbContext db,
    IHubContext<SessionHub> hub) : ApiControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(Guid campaignId)
    {
        var isDm = await db.Campaigns.AnyAsync(c => c.Id == campaignId && c.OwnerId == CurrentUserId);
        return Ok(await svc.GetAsync(campaignId, isDm));
    }

    [HttpPost]
    public async Task<IActionResult> Create(Guid campaignId, [FromBody] CreateHandoutDto dto)
    {
        if (!await IsDm(campaignId)) return Forbid();
        var handout = await svc.CreateAsync(campaignId, CurrentUserId, dto);
        return StatusCode(201, handout);
    }

    [HttpPut("{handoutId:guid}")]
    public async Task<IActionResult> Update(Guid campaignId, Guid handoutId, [FromBody] UpdateHandoutDto dto)
    {
        if (!await IsDm(campaignId)) return Forbid();
        var result = await svc.UpdateAsync(handoutId, dto);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost("{handoutId:guid}/reveal")]
    public async Task<IActionResult> ToggleReveal(Guid campaignId, Guid handoutId)
    {
        if (!await IsDm(campaignId)) return Forbid();
        var result = await svc.ToggleRevealAsync(handoutId);
        if (result is null) return NotFound();

        // Broadcast to active session if one exists
        var session = await db.GameSessions
            .Where(s => s.CampaignId == campaignId && s.Status == "active")
            .FirstOrDefaultAsync();
        if (session is not null)
            await hub.Clients.Group(session.Id.ToString())
                .SendAsync(HandoutEvents.HandoutRevealChanged, result);

        return Ok(result);
    }

    [HttpDelete("{handoutId:guid}")]
    public async Task<IActionResult> Delete(Guid campaignId, Guid handoutId)
    {
        if (!await IsDm(campaignId)) return Forbid();
        return await svc.DeleteAsync(handoutId) ? NoContent() : NotFound();
    }

    private async Task<bool> IsDm(Guid campaignId) =>
        await db.Campaigns.AnyAsync(c => c.Id == campaignId && c.OwnerId == CurrentUserId);
}

public static class HandoutEvents
{
    public const string HandoutRevealChanged = "HandoutRevealChanged";
}
