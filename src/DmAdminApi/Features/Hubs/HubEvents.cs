namespace DmAdminApi.Features.Hubs;

/// <summary>Event name constants shared between the hub and services.</summary>
public static class HubEvents
{
    public const string EntityCreated       = "EntityCreated";
    public const string EntityUpdated       = "EntityUpdated";
    public const string EntityDeleted       = "EntityDeleted";
    public const string PermissionsChanged  = "PermissionsChanged";
    public const string PresenceUpdated     = "PresenceUpdated";
}

public static class HubGroups
{
    public static string Campaign(Guid campaignId)         => $"campaign:{campaignId}";
    public static string CampaignRole(Guid campaignId, string roleName) => $"campaign:{campaignId}:role:{roleName}";
}
