using System.Security.Claims;
using System.Text.Json;
using DmAdminApi.Common.Controllers;
using DmAdminApi.Features.Permissions;
using DmAdminApi.Features.Sessions.Dtos;
using DmAdminApi.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace DmAdminApi.Features.Sessions;

[Authorize]
[Route("api/campaigns/{campaignId:guid}/sessions")]
public class SessionController(
    SessionService sessions,
    ChatService chat,
    PermissionService permissions,
    IHubContext<SessionHub> hubContext,
    SessionPresenceTracker presence,
    AppDbContext db) : ApiControllerBase
{
    private string CurrentDisplayName =>
        User.FindFirstValue("display_name") ?? "Usuario";

    // ── Sesiones ──────────────────────────────────────────────────────────────

    [HttpPost("")]
    public async Task<IActionResult> CreateSession(Guid campaignId, [FromBody] CreateSessionDto dto)
    {
        if (!await permissions.IsCampaignOwnerAsync(campaignId, CurrentUserId))
            return Forbid();

        try
        {
            var result = await sessions.CreateAsync(campaignId, CurrentUserId, dto);
            return CreatedAtAction(nameof(GetDetail), new { campaignId, sid = result.Id }, result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("{sid:guid}/end")]
    public async Task<IActionResult> EndSession(Guid campaignId, Guid sid)
    {
        if (!await permissions.IsCampaignOwnerAsync(campaignId, CurrentUserId))
            return Forbid();

        try
        {
            await sessions.EndAsync(sid, campaignId);
            await hubContext.Clients.Group(sid.ToString()).SendAsync("SessionEnded");
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    [HttpGet("")]
    public async Task<IActionResult> GetAll(Guid campaignId)
    {
        if (!await IsMemberOrOwner(campaignId)) return Forbid();
        return Ok(await sessions.GetAllAsync(campaignId));
    }

    [HttpGet("{sid:guid}")]
    public async Task<IActionResult> GetDetail(Guid campaignId, Guid sid)
    {
        if (!await IsMemberOrOwner(campaignId)) return Forbid();

        try { return Ok(await sessions.GetDetailAsync(sid)); }
        catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
    }

    [HttpGet("{sid:guid}/messages")]
    public async Task<IActionResult> GetMessages(
        Guid campaignId, Guid sid,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 50)
    {
        if (!await IsMemberOrOwner(campaignId)) return Forbid();
        return Ok(await sessions.GetMessagesPagedAsync(sid, page, pageSize));
    }

    // ── Mensajes ──────────────────────────────────────────────────────────────

    [HttpPost("{sid:guid}/messages")]
    public async Task<IActionResult> SendMessage(
        Guid campaignId, Guid sid, [FromBody] SendMessageDto dto)
    {
        if (!await IsMemberOrOwner(campaignId)) return Forbid();

        var session = await db.GameSessions
            .FirstOrDefaultAsync(s => s.Id == sid && s.CampaignId == campaignId);
        if (session is null) return NotFound(new { error = "Sesión no encontrada." });
        if (session.Status != "active") return BadRequest(new { error = "La sesión no está activa." });

        try
        {
            var message = await chat.ProcessMessageAsync(sid, CurrentUserId, dto);

            var msgDto = new ChatMessageDto(
                message.Id, sid, CurrentUserId, CurrentDisplayName,
                message.Type, message.Content,
                ChatService.DeserializeDiceResult(message.DiceResultJson),
                message.IsSecret, message.CreatedAt);

            if (message.IsSecret)
            {
                // Deliver only to the shooter's connections
                foreach (var conn in presence.GetConnectionIds(sid, CurrentUserId))
                    await hubContext.Clients.Client(conn).SendAsync("MessageReceived", msgDto);

                // Deliver also to the DM (campaign owner) if different from shooter
                var campaign = await db.Campaigns.FindAsync(session.CampaignId);
                if (campaign is not null && campaign.OwnerId != CurrentUserId)
                {
                    foreach (var conn in presence.GetConnectionIds(sid, campaign.OwnerId))
                        await hubContext.Clients.Client(conn).SendAsync("MessageReceived", msgDto);
                }
            }
            else
            {
                await hubContext.Clients.Group(sid.ToString()).SendAsync("MessageReceived", msgDto);
            }

            return StatusCode(201, msgDto);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private async Task<bool> IsMemberOrOwner(Guid campaignId) =>
        await permissions.IsCampaignOwnerAsync(campaignId, CurrentUserId) ||
        await permissions.GetMemberAsync(campaignId, CurrentUserId) is not null;
}
