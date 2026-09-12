namespace DmAdminApi.Features.Sessions;

public record SessionPresenceDto(Guid UserId, string DisplayName);

/// <summary>
/// Singleton that tracks which users are connected to which game sessions.
/// Thread-safe. Handles multiple browser tabs per user.
/// </summary>
public class SessionPresenceTracker
{
    // connectionId → (sessionId, userId, displayName)
    private readonly Dictionary<string, (Guid SessionId, Guid UserId, string DisplayName)> _connections = new();
    private readonly Lock _lock = new();

    public void Add(string connectionId, Guid sessionId, Guid userId, string displayName)
    {
        lock (_lock)
        {
            _connections[connectionId] = (sessionId, userId, displayName);
        }
    }

    /// <summary>Removes a connection. Returns the sessionId it was in and the updated presence list.</summary>
    public (Guid SessionId, SessionPresenceDto[] Presence) Remove(string connectionId)
    {
        lock (_lock)
        {
            if (_connections.Remove(connectionId, out var entry))
            {
                var presence = GetPresenceInternal(entry.SessionId);
                return (entry.SessionId, presence);
            }
            return (Guid.Empty, []);
        }
    }

    public SessionPresenceDto[] GetPresence(Guid sessionId)
    {
        lock (_lock) return GetPresenceInternal(sessionId);
    }

    /// <summary>Returns all SignalR connection IDs for a given user in a given session.</summary>
    public List<string> GetConnectionIds(Guid sessionId, Guid userId)
    {
        lock (_lock)
        {
            return _connections
                .Where(kv => kv.Value.SessionId == sessionId && kv.Value.UserId == userId)
                .Select(kv => kv.Key)
                .ToList();
        }
    }

    private SessionPresenceDto[] GetPresenceInternal(Guid sessionId) =>
        _connections.Values
            .Where(v => v.SessionId == sessionId)
            .GroupBy(v => v.UserId)
            .Select(g => new SessionPresenceDto(g.Key, g.First().DisplayName))
            .ToArray();
}
