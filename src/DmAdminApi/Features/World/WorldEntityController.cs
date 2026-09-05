using DmAdminApi.Common.Controllers;
using DmAdminApi.Features.Permissions;
using DmAdminApi.Features.World.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DmAdminApi.Features.World;

[Authorize]
[Route("api/campaigns/{campaignId:guid}/entities")]
public class WorldEntityController(WorldEntityService worldEntities, PermissionService permissions) : ApiControllerBase
{
    [HttpGet("types")]
    public async Task<IActionResult> GetTypes(Guid campaignId)
    {
        if (!await CanAccessCampaignAsync(campaignId)) return Forbid();
        return Ok(await worldEntities.GetEntityTypesAsync(campaignId));
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(Guid campaignId)
    {
        if (!await CanAccessCampaignAsync(campaignId)) return Forbid();
        return Ok(await worldEntities.GetEntitiesAsync(campaignId, CurrentUserId));
    }

    [HttpGet("{entityId:guid}")]
    public async Task<IActionResult> GetOne(Guid campaignId, Guid entityId)
    {
        try { return Ok(await worldEntities.GetEntityAsync(entityId, campaignId, CurrentUserId)); }
        catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
        catch (UnauthorizedAccessException) { return NotFound(); }  // Don't reveal existence
    }

    [HttpPost]
    public async Task<IActionResult> Create(Guid campaignId, [FromBody] CreateWorldEntityDto dto)
    {
        if (!await permissions.IsCampaignOwnerAsync(campaignId, CurrentUserId))
        {
            var member = await permissions.GetMemberAsync(campaignId, CurrentUserId);
            if (member is null || member.Role.Name == Infrastructure.Data.Entities.SystemRoles.Spectator)
                return Forbid();
        }

        try
        {
            var result = await worldEntities.CreateAsync(campaignId, dto, CurrentUserId);
            return CreatedAtAction(nameof(GetOne), new { campaignId, entityId = result.Id }, result);
        }
        catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
    }

    [HttpPut("{entityId:guid}")]
    public async Task<IActionResult> Update(Guid campaignId, Guid entityId, [FromBody] UpdateWorldEntityDto dto)
    {
        try { return Ok(await worldEntities.UpdateAsync(entityId, campaignId, dto, CurrentUserId)); }
        catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
        catch (UnauthorizedAccessException ex) { return Forbid(); }
        catch (InvalidOperationException ex) when (ex.Message == "conflict")
        {
            return Conflict(new { error = "conflict", message = "Entity was modified by another user." });
        }
    }

    [HttpDelete("{entityId:guid}")]
    public async Task<IActionResult> Delete(Guid campaignId, Guid entityId)
    {
        if (!await permissions.IsCampaignOwnerAsync(campaignId, CurrentUserId))
        {
            var member = await permissions.GetMemberAsync(campaignId, CurrentUserId);
            if (member?.Role.Name != Infrastructure.Data.Entities.SystemRoles.CoDm)
                return Forbid();
        }

        try { await worldEntities.DeleteAsync(entityId, campaignId); return NoContent(); }
        catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
    }

    // ── Permissions (DM only) ─────────────────────────────────────────────────

    [HttpGet("{entityId:guid}/permissions")]
    public async Task<IActionResult> GetPermissions(Guid campaignId, Guid entityId)
    {
        if (!await permissions.IsCampaignOwnerAsync(campaignId, CurrentUserId)) return Forbid();
        return Ok(await worldEntities.GetPermissionsAsync(entityId));
    }

    [HttpPut("{entityId:guid}/permissions")]
    public async Task<IActionResult> SetPermission(Guid campaignId, Guid entityId, [FromBody] SetPermissionDto dto)
    {
        if (!await permissions.IsCampaignOwnerAsync(campaignId, CurrentUserId)) return Forbid();
        return Ok(await worldEntities.SetPermissionAsync(entityId, campaignId, dto));
    }

    // ── Helper ────────────────────────────────────────────────────────────────

    private async Task<bool> CanAccessCampaignAsync(Guid campaignId)
    {
        return await permissions.IsCampaignOwnerAsync(campaignId, CurrentUserId)
            || await permissions.GetMemberAsync(campaignId, CurrentUserId) is not null;
    }
}
