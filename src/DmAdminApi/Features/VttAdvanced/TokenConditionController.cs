using DmAdminApi.Common.Controllers;
using DmAdminApi.Features.Permissions;
using DmAdminApi.Features.VttAdvanced.Dtos;
using DmAdminApi.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DmAdminApi.Features.VttAdvanced;

[Authorize]
[Route("api/campaigns/{campaignId:guid}/sessions/{sessionId:guid}/scenes/{sceneId:guid}/tokens/{tokenId:guid}/conditions")]
public class TokenConditionController(
    TokenConditionService conditionService,
    PermissionService permissions,
    AppDbContext db) : ApiControllerBase
{
    [HttpPost("")]
    public async Task<IActionResult> AddCondition(
        Guid campaignId, Guid sessionId, Guid sceneId, Guid tokenId,
        [FromBody] AddConditionDto dto)
    {
        if (!await permissions.IsCampaignOwnerAsync(campaignId, CurrentUserId)) return Forbid();
        if (!await TokenBelongsToScene(tokenId, sceneId)) return NotFound();
        try
        {
            var conditions = await conditionService.AddConditionAsync(tokenId, sessionId, dto);
            return StatusCode(201, conditions);
        }
        catch (InvalidOperationException ex) { return Conflict(new { error = ex.Message }); }
    }

    [HttpDelete("{conditionId:guid}")]
    public async Task<IActionResult> RemoveCondition(
        Guid campaignId, Guid sessionId, Guid sceneId, Guid tokenId, Guid conditionId)
    {
        if (!await permissions.IsCampaignOwnerAsync(campaignId, CurrentUserId)) return Forbid();
        try
        {
            var conditions = await conditionService.RemoveConditionAsync(conditionId, tokenId, sessionId);
            return Ok(conditions);
        }
        catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
    }

    private async Task<bool> TokenBelongsToScene(Guid tokenId, Guid sceneId) =>
        await db.MapTokens.AnyAsync(t => t.Id == tokenId && t.SceneId == sceneId);
}
