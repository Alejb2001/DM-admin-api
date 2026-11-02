using DmAdminApi.Common.Controllers;
using DmAdminApi.Features.Sessions;
using DmAdminApi.Features.Tables.Dtos;
using DmAdminApi.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace DmAdminApi.Features.Tables;

[Route("api/campaigns/{campaignId:guid}/tables")]
public class TableController(TableService svc, AppDbContext db,
    IHubContext<SessionHub> hub) : ApiControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(Guid campaignId) =>
        Ok(await svc.GetAllAsync(campaignId));

    [HttpPost]
    public async Task<IActionResult> Create(Guid campaignId, [FromBody] CreateTableDto dto)
    {
        if (!await IsMember(campaignId)) return Forbid();
        return StatusCode(201, await svc.CreateAsync(campaignId, CurrentUserId, dto));
    }

    [HttpPut("{tableId:guid}")]
    public async Task<IActionResult> Update(Guid campaignId, Guid tableId, [FromBody] UpdateTableDto dto)
    {
        var r = await svc.UpdateAsync(tableId, dto);
        return r is null ? NotFound() : Ok(r);
    }

    [HttpDelete("{tableId:guid}")]
    public async Task<IActionResult> Delete(Guid campaignId, Guid tableId) =>
        await svc.DeleteAsync(tableId) ? NoContent() : NotFound();

    [HttpPost("{tableId:guid}/entries")]
    public async Task<IActionResult> AddEntry(Guid campaignId, Guid tableId, [FromBody] CreateEntryDto dto) =>
        StatusCode(201, await svc.AddEntryAsync(tableId, dto));

    [HttpPut("{tableId:guid}/entries/{entryId:guid}")]
    public async Task<IActionResult> UpdateEntry(Guid campaignId, Guid tableId, Guid entryId,
        [FromBody] UpdateEntryDto dto)
    {
        var r = await svc.UpdateEntryAsync(entryId, dto);
        return r is null ? NotFound() : Ok(r);
    }

    [HttpDelete("{tableId:guid}/entries/{entryId:guid}")]
    public async Task<IActionResult> DeleteEntry(Guid campaignId, Guid tableId, Guid entryId) =>
        await svc.DeleteEntryAsync(entryId) ? NoContent() : NotFound();

    [HttpPost("{tableId:guid}/roll")]
    public async Task<IActionResult> Roll(Guid campaignId, Guid tableId,
        [FromQuery] Guid? sessionId)
    {
        var result = await svc.RollAsync(tableId);
        if (result is null) return NotFound();

        // If a session is active, post result to chat
        if (sessionId.HasValue)
        {
            await hub.Clients.Group(sessionId.Value.ToString())
                .SendAsync(TableEvents.TableRolled, result);
        }
        return Ok(result);
    }

    private async Task<bool> IsMember(Guid campaignId)
    {
        var isOwner = await db.Campaigns.AnyAsync(c => c.Id == campaignId && c.OwnerId == CurrentUserId);
        var isMember = await db.CampaignMembers.AnyAsync(m => m.CampaignId == campaignId && m.UserId == CurrentUserId);
        return isOwner || isMember;
    }
}

public static class TableEvents
{
    public const string TableRolled = "TableRolled";
}
