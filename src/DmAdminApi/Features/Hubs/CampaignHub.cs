using System.Security.Claims;
using DmAdminApi.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace DmAdminApi.Features.Hubs;

[Authorize]
public class CampaignHub(PresenceTracker presence, AppDbContext db) : Hub
{
    private Guid CurrentUserId =>
        Guid.Parse(Context.User!.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private string CurrentDisplayName =>
        Context.User!.FindFirstValue("display_name") ?? "Usuario";

    /// <summary>
    /// Client calls this after connecting to join a specific campaign's real-time group.
    /// Adds to the base campaign group and to their role-specific sub-group.
    /// </summary>
    public async Task JoinCampaign(string campaignId)
    {
        if (!Guid.TryParse(campaignId, out var id)) return;

        var userId = CurrentUserId;

        // Always join the base campaign group
        await Groups.AddToGroupAsync(Context.ConnectionId, HubGroups.Campaign(id));

        // Join role-specific sub-group if member (not DM)
        var member = await db.CampaignMembers
            .Include(m => m.Role)
            .FirstOrDefaultAsync(m => m.CampaignId == id && m.UserId == userId);

        if (member is not null)
            await Groups.AddToGroupAsync(Context.ConnectionId, HubGroups.CampaignRole(id, member.Role.Name));

        // Track presence
        presence.Add(Context.ConnectionId, id, userId, CurrentDisplayName);

        // Broadcast updated presence to all in campaign
        await Clients.Group(HubGroups.Campaign(id))
            .SendAsync(HubEvents.PresenceUpdated, presence.GetPresence(id));
    }

    /// <summary>Called by client when navigating away from a campaign (optional — OnDisconnectedAsync handles cleanup).</summary>
    public async Task LeaveCampaign(string campaignId)
    {
        if (!Guid.TryParse(campaignId, out var id)) return;

        presence.Remove(Context.ConnectionId);
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, HubGroups.Campaign(id));

        await Clients.Group(HubGroups.Campaign(id))
            .SendAsync(HubEvents.PresenceUpdated, presence.GetPresence(id));
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var campaignId = presence.Remove(Context.ConnectionId);
        if (campaignId.HasValue)
        {
            await Clients.Group(HubGroups.Campaign(campaignId.Value))
                .SendAsync(HubEvents.PresenceUpdated, presence.GetPresence(campaignId.Value));
        }
        await base.OnDisconnectedAsync(exception);
    }
}
