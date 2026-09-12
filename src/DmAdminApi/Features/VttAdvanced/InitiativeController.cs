using DmAdminApi.Common.Controllers;
using DmAdminApi.Features.Permissions;
using DmAdminApi.Features.VttAdvanced.Dtos;
using DmAdminApi.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DmAdminApi.Features.VttAdvanced;

[Authorize]
[Route("api/campaigns/{campaignId:guid}/sessions/{sessionId:guid}/initiative")]
public class InitiativeController(
    InitiativeService initiative,
    PermissionService permissions,
    AppDbContext db) : ApiControllerBase
{
    [HttpGet("")]
    public async Task<IActionResult> GetEntries(Guid campaignId, Guid sessionId)
    {
        if (!await IsMemberOrOwner(campaignId)) return Forbid();
        if (!await SessionBelongsToCampaign(sessionId, campaignId)) return NotFound();
        return Ok(await initiative.GetEntriesAsync(sessionId));
    }

    [HttpPost("")]
    public async Task<IActionResult> AddEntry(
        Guid campaignId, Guid sessionId, [FromBody] CreateInitiativeEntryDto dto)
    {
        if (!await permissions.IsCampaignOwnerAsync(campaignId, CurrentUserId)) return Forbid();
        if (!await SessionBelongsToCampaign(sessionId, campaignId)) return NotFound();
        var list = await initiative.AddEntryAsync(sessionId, dto);
        return StatusCode(201, list);
    }

    [HttpPatch("order")]
    public async Task<IActionResult> ReorderEntries(
        Guid campaignId, Guid sessionId, [FromBody] UpdateInitiativeOrderDto dto)
    {
        if (!await permissions.IsCampaignOwnerAsync(campaignId, CurrentUserId)) return Forbid();
        var list = await initiative.ReorderAsync(sessionId, dto);
        return Ok(list);
    }

    [HttpPost("{entryId:guid}/activate")]
    public async Task<IActionResult> SetActive(Guid campaignId, Guid sessionId, Guid entryId)
    {
        if (!await permissions.IsCampaignOwnerAsync(campaignId, CurrentUserId)) return Forbid();
        try
        {
            var list = await initiative.SetActiveAsync(sessionId, entryId);
            return Ok(list);
        }
        catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
    }

    [HttpDelete("{entryId:guid}")]
    public async Task<IActionResult> RemoveEntry(Guid campaignId, Guid sessionId, Guid entryId)
    {
        if (!await permissions.IsCampaignOwnerAsync(campaignId, CurrentUserId)) return Forbid();
        try
        {
            var list = await initiative.RemoveEntryAsync(sessionId, entryId);
            return Ok(list);
        }
        catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
    }

    [HttpDelete("")]
    public async Task<IActionResult> ClearInitiative(Guid campaignId, Guid sessionId)
    {
        if (!await permissions.IsCampaignOwnerAsync(campaignId, CurrentUserId)) return Forbid();
        await initiative.ClearAsync(sessionId);
        return NoContent();
    }

    private async Task<bool> IsMemberOrOwner(Guid campaignId) =>
        await permissions.IsCampaignOwnerAsync(campaignId, CurrentUserId) ||
        await permissions.GetMemberAsync(campaignId, CurrentUserId) is not null;

    private async Task<bool> SessionBelongsToCampaign(Guid sessionId, Guid campaignId) =>
        await db.GameSessions.AnyAsync(s => s.Id == sessionId && s.CampaignId == campaignId);
}
