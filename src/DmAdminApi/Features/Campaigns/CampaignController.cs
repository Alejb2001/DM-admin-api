using DmAdminApi.Common.Controllers;
using DmAdminApi.Features.Campaigns.Dtos;
using DmAdminApi.Features.Permissions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DmAdminApi.Features.Campaigns;

[Authorize]
[Route("api/campaigns")]
public class CampaignController(CampaignService campaigns, PermissionService permissions) : ApiControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll() =>
        Ok(await campaigns.GetUserCampaignsAsync(CurrentUserId));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetDetail(Guid id)
    {
        if (!await permissions.IsCampaignOwnerAsync(id, CurrentUserId) &&
            await permissions.GetMemberAsync(id, CurrentUserId) is null)
            return Forbid();

        try { return Ok(await campaigns.GetCampaignDetailAsync(id)); }
        catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateCampaignDto dto)
    {
        var result = await campaigns.CreateAsync(dto, CurrentUserId);
        return CreatedAtAction(nameof(GetDetail), new { id = result.Id }, result);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateCampaignDto dto)
    {
        if (!await permissions.IsCampaignOwnerAsync(id, CurrentUserId)) return Forbid();
        try { return Ok(await campaigns.UpdateAsync(id, dto)); }
        catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        if (!await permissions.IsCampaignOwnerAsync(id, CurrentUserId)) return Forbid();
        try { await campaigns.DeleteAsync(id); return NoContent(); }
        catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
    }

    // ── Invitations ───────────────────────────────────────────────────────────

    [HttpPost("{id:guid}/invitations")]
    public async Task<IActionResult> CreateInvitation(Guid id, [FromBody] CreateInvitationDto dto)
    {
        if (!await permissions.IsCampaignOwnerAsync(id, CurrentUserId)) return Forbid();
        try { return Ok(await campaigns.CreateInvitationAsync(id, dto)); }
        catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
    }

    [HttpPost("join")]
    public async Task<IActionResult> Join([FromBody] JoinCampaignDto dto)
    {
        try { return Ok(await campaigns.JoinCampaignAsync(dto.Token, CurrentUserId)); }
        catch (InvalidOperationException ex) { return BadRequest(new { error = ex.Message }); }
    }

    [HttpPost("join-by-code")]
    public async Task<IActionResult> JoinByCode([FromBody] JoinByCodeDto dto)
    {
        try { return Ok(await campaigns.JoinByCodeAsync(dto.Code, CurrentUserId)); }
        catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
        catch (InvalidOperationException ex) { return BadRequest(new { error = ex.Message }); }
    }

    [HttpPost("{id:guid}/regenerate-code")]
    public async Task<IActionResult> RegenerateCode(Guid id)
    {
        if (!await permissions.IsCampaignOwnerAsync(id, CurrentUserId)) return Forbid();
        try { return Ok(new { joinCode = await campaigns.RegenerateCodeAsync(id) }); }
        catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
    }

    // ── Members ───────────────────────────────────────────────────────────────

    [HttpPut("{id:guid}/members/{memberId:guid}/role")]
    public async Task<IActionResult> UpdateMemberRole(Guid id, Guid memberId, [FromBody] UpdateMemberRoleDto dto)
    {
        if (!await permissions.IsCampaignOwnerAsync(id, CurrentUserId)) return Forbid();
        try { await campaigns.UpdateMemberRoleAsync(id, memberId, dto); return NoContent(); }
        catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
    }

    [HttpDelete("{id:guid}/members/{memberId:guid}")]
    public async Task<IActionResult> RemoveMember(Guid id, Guid memberId)
    {
        if (!await permissions.IsCampaignOwnerAsync(id, CurrentUserId)) return Forbid();
        try { await campaigns.RemoveMemberAsync(id, memberId); return NoContent(); }
        catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
    }

    [HttpPost("{id:guid}/leave")]
    public async Task<IActionResult> Leave(Guid id)
    {
        try { await campaigns.LeaveAsync(id, CurrentUserId); return NoContent(); }
        catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
    }
}
