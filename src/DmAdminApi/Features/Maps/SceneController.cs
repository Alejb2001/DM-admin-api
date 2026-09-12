using DmAdminApi.Common.Controllers;
using DmAdminApi.Features.Maps.Dtos;
using DmAdminApi.Features.Permissions;
using DmAdminApi.Features.Sessions;
using DmAdminApi.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace DmAdminApi.Features.Maps;

[Authorize]
[Route("api/campaigns/{campaignId:guid}/sessions/{sessionId:guid}/scenes")]
public class SceneController(
    SceneService scenes,
    PermissionService permissions,
    IHubContext<SessionHub> hub,
    AppDbContext db) : ApiControllerBase
{
    // ── Scenes ────────────────────────────────────────────────────────────────

    [HttpGet("")]
    public async Task<IActionResult> GetScenes(Guid campaignId, Guid sessionId)
    {
        if (!await IsMemberOrOwner(campaignId)) return Forbid();
        if (!await SessionBelongsToCampaign(sessionId, campaignId)) return NotFound();
        return Ok(await scenes.GetScenesAsync(sessionId));
    }

    [HttpPost("")]
    public async Task<IActionResult> CreateScene(Guid campaignId, Guid sessionId, [FromBody] CreateSceneDto dto)
    {
        if (!await permissions.IsCampaignOwnerAsync(campaignId, CurrentUserId)) return Forbid();
        if (!await SessionBelongsToCampaign(sessionId, campaignId)) return NotFound();

        var scene = await scenes.CreateSceneAsync(sessionId, dto);
        await hub.Clients.Group(sessionId.ToString()).SendAsync(MapEvents.SceneUpdated, scene);
        return StatusCode(201, scene);
    }

    [HttpPut("{sceneId:guid}")]
    public async Task<IActionResult> UpdateScene(
        Guid campaignId, Guid sessionId, Guid sceneId, [FromBody] UpdateSceneDto dto)
    {
        if (!await permissions.IsCampaignOwnerAsync(campaignId, CurrentUserId)) return Forbid();
        try
        {
            var scene = await scenes.UpdateSceneAsync(sceneId, dto);
            await hub.Clients.Group(sessionId.ToString()).SendAsync(MapEvents.SceneUpdated, scene);
            return Ok(scene);
        }
        catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
    }

    [HttpPost("{sceneId:guid}/activate")]
    public async Task<IActionResult> ActivateScene(Guid campaignId, Guid sessionId, Guid sceneId)
    {
        if (!await permissions.IsCampaignOwnerAsync(campaignId, CurrentUserId)) return Forbid();
        try
        {
            var scene = await scenes.ActivateSceneAsync(sceneId, sessionId);
            await hub.Clients.Group(sessionId.ToString()).SendAsync(MapEvents.SceneActivated, scene);
            return Ok(scene);
        }
        catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
    }

    [HttpDelete("{sceneId:guid}")]
    public async Task<IActionResult> DeleteScene(Guid campaignId, Guid sessionId, Guid sceneId)
    {
        if (!await permissions.IsCampaignOwnerAsync(campaignId, CurrentUserId)) return Forbid();
        try
        {
            await scenes.DeleteSceneAsync(sceneId);
            return NoContent();
        }
        catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
    }

    // ── Tokens ────────────────────────────────────────────────────────────────

    [HttpGet("{sceneId:guid}/tokens")]
    public async Task<IActionResult> GetTokens(Guid campaignId, Guid sessionId, Guid sceneId)
    {
        if (!await IsMemberOrOwner(campaignId)) return Forbid();
        return Ok(await scenes.GetTokensAsync(sceneId));
    }

    [HttpPost("{sceneId:guid}/tokens")]
    public async Task<IActionResult> AddToken(
        Guid campaignId, Guid sessionId, Guid sceneId, [FromBody] CreateTokenDto dto)
    {
        if (!await permissions.IsCampaignOwnerAsync(campaignId, CurrentUserId)) return Forbid();

        var token = await scenes.AddTokenAsync(sceneId, dto);
        await hub.Clients.Group(sessionId.ToString()).SendAsync(MapEvents.TokenAdded, token);
        return StatusCode(201, token);
    }

    [HttpPatch("{sceneId:guid}/tokens/{tokenId:guid}/move")]
    public async Task<IActionResult> MoveToken(
        Guid campaignId, Guid sessionId, Guid sceneId, Guid tokenId, [FromBody] MoveTokenDto dto)
    {
        if (!await IsMemberOrOwner(campaignId)) return Forbid();
        try
        {
            var isDm = await permissions.IsCampaignOwnerAsync(campaignId, CurrentUserId);
            var token = await scenes.MoveTokenAsync(tokenId, dto, CurrentUserId, isDm);
            await hub.Clients.Group(sessionId.ToString())
                .SendAsync(MapEvents.TokenMoved, new { tokenId = token.Id, x = token.X, y = token.Y });
            return Ok(token);
        }
        catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
        catch (UnauthorizedAccessException) { return Forbid(); }
    }

    [HttpPut("{sceneId:guid}/tokens/{tokenId:guid}")]
    public async Task<IActionResult> UpdateToken(
        Guid campaignId, Guid sessionId, Guid sceneId, Guid tokenId, [FromBody] UpdateTokenDto dto)
    {
        if (!await permissions.IsCampaignOwnerAsync(campaignId, CurrentUserId)) return Forbid();
        try
        {
            var token = await scenes.UpdateTokenAsync(tokenId, dto);
            await hub.Clients.Group(sessionId.ToString()).SendAsync(MapEvents.TokenUpdated, token);
            return Ok(token);
        }
        catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
    }

    [HttpDelete("{sceneId:guid}/tokens/{tokenId:guid}")]
    public async Task<IActionResult> DeleteToken(
        Guid campaignId, Guid sessionId, Guid sceneId, Guid tokenId)
    {
        if (!await permissions.IsCampaignOwnerAsync(campaignId, CurrentUserId)) return Forbid();
        try
        {
            await scenes.DeleteTokenAsync(tokenId);
            await hub.Clients.Group(sessionId.ToString()).SendAsync(MapEvents.TokenRemoved, tokenId);
            return NoContent();
        }
        catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private async Task<bool> IsMemberOrOwner(Guid campaignId) =>
        await permissions.IsCampaignOwnerAsync(campaignId, CurrentUserId) ||
        await permissions.GetMemberAsync(campaignId, CurrentUserId) is not null;

    private async Task<bool> SessionBelongsToCampaign(Guid sessionId, Guid campaignId) =>
        await db.GameSessions.AnyAsync(s => s.Id == sessionId && s.CampaignId == campaignId);
}
