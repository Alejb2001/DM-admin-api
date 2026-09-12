using System.Security.Claims;
using DmAdminApi.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace DmAdminApi.Features.Sessions;

[Authorize]
public class SessionHub(SessionPresenceTracker presence, AppDbContext db) : Hub
{
    private Guid CurrentUserId =>
        Guid.Parse(Context.User!.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private string CurrentDisplayName =>
        Context.User!.FindFirstValue("display_name") ?? "Usuario";

    public async Task JoinSession(string sessionId)
    {
        if (!Guid.TryParse(sessionId, out var sid)) return;

        var session = await db.GameSessions.FirstOrDefaultAsync(s => s.Id == sid);
        if (session is null) return;

        var userId = CurrentUserId;
        var isOwner = await db.Campaigns.AnyAsync(c => c.Id == session.CampaignId && c.OwnerId == userId);
        var isMember = await db.CampaignMembers.AnyAsync(m => m.CampaignId == session.CampaignId && m.UserId == userId);
        if (!isOwner && !isMember) return;

        await Groups.AddToGroupAsync(Context.ConnectionId, sid.ToString());
        presence.Add(Context.ConnectionId, sid, userId, CurrentDisplayName);

        await Clients.Group(sid.ToString())
            .SendAsync("PresenceUpdated", presence.GetPresence(sid));
    }

    public async Task LeaveSession(string sessionId)
    {
        if (!Guid.TryParse(sessionId, out var sid)) return;

        await Groups.RemoveFromGroupAsync(Context.ConnectionId, sid.ToString());
        var (_, presenceList) = presence.Remove(Context.ConnectionId);

        await Clients.Group(sid.ToString())
            .SendAsync("PresenceUpdated", presenceList);
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var (sessionId, presenceList) = presence.Remove(Context.ConnectionId);
        if (sessionId != Guid.Empty)
        {
            await Clients.Group(sessionId.ToString())
                .SendAsync("PresenceUpdated", presenceList);
        }
        await base.OnDisconnectedAsync(exception);
    }
}

public static class MapEvents
{
    public const string SceneActivated = "SceneActivated";
    public const string SceneUpdated   = "SceneUpdated";
    public const string TokenAdded     = "TokenAdded";
    public const string TokenMoved     = "TokenMoved";
    public const string TokenUpdated   = "TokenUpdated";
    public const string TokenRemoved   = "TokenRemoved";
}

public static class SheetEvents
{
    public const string ResourceUpdated = "ResourceUpdated";
}
