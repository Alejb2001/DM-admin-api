using DmAdminApi.Common.Controllers;
using DmAdminApi.Features.Permissions;
using DmAdminApi.Features.World.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DmAdminApi.Features.World;

[Authorize]
[Route("api/campaigns/{campaignId:guid}/entity-types")]
public class EntityTypeController(EntityTypeService entityTypes, PermissionService permissions) : ApiControllerBase
{
    // GET all types with their fields (for DM management view)
    [HttpGet]
    public async Task<IActionResult> GetAll(Guid campaignId)
    {
        if (!await CanAccessCampaignAsync(campaignId)) return Forbid();
        return Ok(await entityTypes.GetTypesWithFieldsAsync(campaignId));
    }

    // POST — create custom entity type (DM only)
    [HttpPost]
    public async Task<IActionResult> Create(Guid campaignId, [FromBody] CreateEntityTypeDto dto)
    {
        if (!await permissions.IsCampaignOwnerAsync(campaignId, CurrentUserId)) return Forbid();
        try
        {
            var result = await entityTypes.CreateTypeAsync(campaignId, dto);
            return Created("", result);
        }
        catch (Exception ex) { return BadRequest(new { error = ex.Message }); }
    }

    // PUT — update custom entity type (DM only)
    [HttpPut("{typeId:guid}")]
    public async Task<IActionResult> Update(Guid campaignId, Guid typeId, [FromBody] UpdateEntityTypeDto dto)
    {
        if (!await permissions.IsCampaignOwnerAsync(campaignId, CurrentUserId)) return Forbid();
        try { return Ok(await entityTypes.UpdateTypeAsync(typeId, campaignId, dto)); }
        catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
        catch (InvalidOperationException ex) { return BadRequest(new { error = ex.Message }); }
    }

    // DELETE — delete custom entity type (DM only)
    [HttpDelete("{typeId:guid}")]
    public async Task<IActionResult> Delete(Guid campaignId, Guid typeId)
    {
        if (!await permissions.IsCampaignOwnerAsync(campaignId, CurrentUserId)) return Forbid();
        try { await entityTypes.DeleteTypeAsync(typeId, campaignId); return NoContent(); }
        catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
        catch (InvalidOperationException ex) { return BadRequest(new { error = ex.Message }); }
    }

    // ── Fields ────────────────────────────────────────────────────────────────

    [HttpGet("{typeId:guid}/fields")]
    public async Task<IActionResult> GetFields(Guid campaignId, Guid typeId)
    {
        if (!await CanAccessCampaignAsync(campaignId)) return Forbid();
        try { return Ok(await entityTypes.GetFieldsAsync(typeId, campaignId)); }
        catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
    }

    [HttpPost("{typeId:guid}/fields")]
    public async Task<IActionResult> AddField(Guid campaignId, Guid typeId, [FromBody] CreateEntityTypeFieldDto dto)
    {
        if (!await permissions.IsCampaignOwnerAsync(campaignId, CurrentUserId)) return Forbid();
        try { return Created("", await entityTypes.AddFieldAsync(typeId, campaignId, dto)); }
        catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
        catch (Exception ex) { return BadRequest(new { error = ex.Message }); }
    }

    [HttpPut("{typeId:guid}/fields/{fieldId:guid}")]
    public async Task<IActionResult> UpdateField(Guid campaignId, Guid typeId, Guid fieldId, [FromBody] UpdateEntityTypeFieldDto dto)
    {
        if (!await permissions.IsCampaignOwnerAsync(campaignId, CurrentUserId)) return Forbid();
        try { return Ok(await entityTypes.UpdateFieldAsync(typeId, campaignId, fieldId, dto)); }
        catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
        catch (Exception ex) { return BadRequest(new { error = ex.Message }); }
    }

    [HttpDelete("{typeId:guid}/fields/{fieldId:guid}")]
    public async Task<IActionResult> DeleteField(Guid campaignId, Guid typeId, Guid fieldId)
    {
        if (!await permissions.IsCampaignOwnerAsync(campaignId, CurrentUserId)) return Forbid();
        try { await entityTypes.DeleteFieldAsync(typeId, campaignId, fieldId); return NoContent(); }
        catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
    }

    private async Task<bool> CanAccessCampaignAsync(Guid campaignId) =>
        await permissions.IsCampaignOwnerAsync(campaignId, CurrentUserId)
        || await permissions.GetMemberAsync(campaignId, CurrentUserId) is not null;
}
