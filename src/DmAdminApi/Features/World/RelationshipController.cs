using DmAdminApi.Common.Controllers;
using DmAdminApi.Features.Permissions;
using DmAdminApi.Features.World.Dtos;
using DmAdminApi.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DmAdminApi.Features.World;

[Authorize]
[Route("api/campaigns/{campaignId:guid}")]
public class RelationshipController(RelationshipService relationships, PermissionService permissions, AppDbContext db) : ApiControllerBase
{
    // ── Relationship Types ────────────────────────────────────────────────────

    [HttpGet("relationship-types")]
    public async Task<IActionResult> GetTypes(Guid campaignId)
    {
        if (!await CanAccessCampaignAsync(campaignId)) return Forbid();
        return Ok(await relationships.GetRelationshipTypesAsync(campaignId));
    }

    [HttpPost("relationship-types")]
    public async Task<IActionResult> CreateType(Guid campaignId, [FromBody] CreateRelationshipTypeDto dto)
    {
        if (!await IsDmOrCoDmAsync(campaignId)) return Forbid();
        try { return Created("", await relationships.CreateRelationshipTypeAsync(campaignId, dto)); }
        catch (Exception ex) { return BadRequest(new { error = ex.Message }); }
    }

    [HttpPut("relationship-types/{typeId:guid}")]
    public async Task<IActionResult> UpdateType(Guid campaignId, Guid typeId, [FromBody] UpdateRelationshipTypeDto dto)
    {
        if (!await IsDmOrCoDmAsync(campaignId)) return Forbid();
        try { return Ok(await relationships.UpdateRelationshipTypeAsync(typeId, campaignId, dto)); }
        catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
        catch (Exception ex) { return BadRequest(new { error = ex.Message }); }
    }

    [HttpDelete("relationship-types/{typeId:guid}")]
    public async Task<IActionResult> DeleteType(Guid campaignId, Guid typeId)
    {
        if (!await permissions.IsCampaignOwnerAsync(campaignId, CurrentUserId)) return Forbid();
        try { await relationships.DeleteRelationshipTypeAsync(typeId, campaignId); return NoContent(); }
        catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
    }

    // ── Entity Relationships ──────────────────────────────────────────────────

    [HttpGet("entities/{entityId:guid}/relationships")]
    public async Task<IActionResult> GetEntityRelationships(Guid campaignId, Guid entityId)
    {
        if (!await CanAccessCampaignAsync(campaignId)) return Forbid();
        return Ok(await relationships.GetEntityRelationshipsAsync(entityId));
    }

    [HttpPost("entities/{entityId:guid}/relationships")]
    public async Task<IActionResult> CreateEntityRelationship(Guid campaignId, Guid entityId, [FromBody] CreateEntityRelationshipDto dto)
    {
        if (!await IsDmOrCoDmAsync(campaignId)) return Forbid();
        try { return Created("", await relationships.CreateEntityRelationshipAsync(entityId, campaignId, dto)); }
        catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
        catch (InvalidOperationException ex) { return Conflict(new { error = ex.Message }); }
    }

    [HttpDelete("entities/{entityId:guid}/relationships/{relationshipId:guid}")]
    public async Task<IActionResult> DeleteEntityRelationship(Guid campaignId, Guid entityId, Guid relationshipId)
    {
        if (!await IsDmOrCoDmAsync(campaignId)) return Forbid();
        try { await relationships.DeleteEntityRelationshipAsync(relationshipId, campaignId); return NoContent(); }
        catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
    }

    // ── Graph ─────────────────────────────────────────────────────────────────

    [HttpGet("graph")]
    public async Task<IActionResult> GetGraph(Guid campaignId)
    {
        if (!await CanAccessCampaignAsync(campaignId)) return Forbid();

        var query = db.WorldEntities
            .Include(e => e.EntityType)
            .Include(e => e.Permissions).ThenInclude(p => p.Role)
            .Where(e => e.CampaignId == campaignId);

        var visibleQuery = await permissions.FilterVisibleEntitiesAsync(query, campaignId, CurrentUserId);
        return Ok(await relationships.GetGraphAsync(campaignId, visibleQuery));
    }

    // ── Search ─────────────────────────────────────────────────────────────────

    [HttpGet("entities/search")]
    public async Task<IActionResult> Search(Guid campaignId, [FromQuery] string q, [FromServices] WorldEntityService worldEntities)
    {
        if (!await CanAccessCampaignAsync(campaignId)) return Forbid();
        if (string.IsNullOrWhiteSpace(q)) return Ok(new List<object>());
        return Ok(await worldEntities.SearchAsync(campaignId, CurrentUserId, q.Trim()));
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private async Task<bool> CanAccessCampaignAsync(Guid campaignId) =>
        await permissions.IsCampaignOwnerAsync(campaignId, CurrentUserId)
        || await permissions.GetMemberAsync(campaignId, CurrentUserId) is not null;

    private async Task<bool> IsDmOrCoDmAsync(Guid campaignId)
    {
        if (await permissions.IsCampaignOwnerAsync(campaignId, CurrentUserId)) return true;
        var member = await permissions.GetMemberAsync(campaignId, CurrentUserId);
        return member?.Role.Name == Infrastructure.Data.Entities.SystemRoles.CoDm;
    }
}
