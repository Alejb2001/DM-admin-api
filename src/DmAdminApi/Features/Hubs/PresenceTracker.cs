namespace DmAdminApi.Features.Hubs;

/// <summary>
/// Singleton that tracks which users are currently connected to which campaigns.
/// Thread-safe. Handles multiple browser tabs per user correctly.
/// </summary>
public class PresenceTracker
{
    // connectionId → (campaignId, userId, displayName)
    private readonly Dictionary<string, (Guid CampaignId, Guid UserId, string DisplayName)> _connections = new();
    private readonly Lock _lock = new();

    public void Add(string connectionId, Guid campaignId, Guid userId, string displayName)
    {
        lock (_lock)
        {
            _connections[connectionId] = (campaignId, userId, displayName);
        }
    }

    public Guid? Remove(string connectionId)
    {
        lock (_lock)
        {
            if (_connections.Remove(connectionId, out var entry))
                return entry.CampaignId;
            return null;
        }
    }

    public Guid? GetCampaignForConnection(string connectionId)
    {
        lock (_lock)
        {
            return _connections.TryGetValue(connectionId, out var entry) ? entry.CampaignId : null;
        }
    }

    /// <summary>Returns one entry per unique user currently connected to the campaign.</summary>
    public List<PresenceDto> GetPresence(Guid campaignId)
    {
        lock (_lock)
        {
            return _connections.Values
                .Where(v => v.CampaignId == campaignId)
                .GroupBy(v => v.UserId)
                .Select(g => new PresenceDto(g.Key, g.First().DisplayName))
                .ToList();
        }
    }
}

public record PresenceDto(Guid UserId, string DisplayName);
