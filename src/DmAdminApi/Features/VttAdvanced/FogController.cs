using DmAdminApi.Common.Controllers;
using DmAdminApi.Features.Permissions;
using DmAdminApi.Features.VttAdvanced.Dtos;
using DmAdminApi.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DmAdminApi.Features.VttAdvanced;

[Authorize]
[Route("api/campaigns/{campaignId:guid}/sessions/{sessionId:guid}/scenes/{sceneId:guid}/fog")]
public class FogController(
    FogService fog,
    PermissionService permissions,
    AppDbContext db) : ApiControllerBase
{
    [HttpGet("")]
    public async Task<IActionResult> GetZones(Guid campaignId, Guid sessionId, Guid sceneId)
    {
        if (!await IsMemberOrOwner(campaignId)) return Forbid();
        if (!await SceneBelongsToSession(sceneId, sessionId)) return NotFound();
        return Ok(await fog.GetZonesAsync(sceneId));
    }

    [HttpPost("zones")]
    public async Task<IActionResult> AddZone(
        Guid campaignId, Guid sessionId, Guid sceneId, [FromBody] CreateFogZoneDto dto)
    {
        if (!await permissions.IsCampaignOwnerAsync(campaignId, CurrentUserId)) return Forbid();
        if (!await SceneBelongsToSession(sceneId, sessionId)) return NotFound();
        try
        {
            var zone = await fog.AddZoneAsync(sceneId, sessionId, dto);
            return StatusCode(201, zone);
        }
        catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
    }

    [HttpDelete("zones/{zoneId:guid}")]
    public async Task<IActionResult> RemoveZone(
        Guid campaignId, Guid sessionId, Guid sceneId, Guid zoneId)
    {
        if (!await permissions.IsCampaignOwnerAsync(campaignId, CurrentUserId)) return Forbid();
        try
        {
            await fog.RemoveZoneAsync(zoneId, sessionId);
            return NoContent();
        }
        catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
    }

    [HttpDelete("zones")]
    public async Task<IActionResult> ClearFog(Guid campaignId, Guid sessionId, Guid sceneId)
    {
        if (!await permissions.IsCampaignOwnerAsync(campaignId, CurrentUserId)) return Forbid();
        await fog.ClearFogAsync(sceneId, sessionId);
        return NoContent();
    }

    [HttpPatch("toggle")]
    public async Task<IActionResult> ToggleFog(
        Guid campaignId, Guid sessionId, Guid sceneId, [FromBody] ToggleFogDto dto)
    {
        if (!await permissions.IsCampaignOwnerAsync(campaignId, CurrentUserId)) return Forbid();
        try
        {
            await fog.ToggleFogAsync(sceneId, sessionId, dto.FogEnabled);
            return Ok(new { fogEnabled = dto.FogEnabled });
        }
        catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
    }

    private async Task<bool> IsMemberOrOwner(Guid campaignId) =>
        await permissions.IsCampaignOwnerAsync(campaignId, CurrentUserId) ||
        await permissions.GetMemberAsync(campaignId, CurrentUserId) is not null;

    private async Task<bool> SceneBelongsToSession(Guid sceneId, Guid sessionId) =>
        await db.SessionScenes.AnyAsync(s => s.Id == sceneId && s.SessionId == sessionId);
}
