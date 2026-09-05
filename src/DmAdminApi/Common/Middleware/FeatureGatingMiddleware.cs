using System.Security.Claims;
using System.Text.Json;
using DmAdminApi.Infrastructure.Data;
using DmAdminApi.Infrastructure.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace DmAdminApi.Common.Middleware;

public class FeatureGatingMiddleware(RequestDelegate next, PlanLimits limits)
{
    public async Task InvokeAsync(HttpContext ctx, AppDbContext db)
    {
        if (ctx.Request.Method is "POST" or "PUT")
        {
            var path = ctx.Request.Path.Value ?? "";
            var userId = GetUserId(ctx);
            var tier = GetTier(ctx);

            if (userId.HasValue && tier is not null)
            {
                var error = await CheckLimitsAsync(path, tier, userId.Value, db);
                if (error is not null)
                {
                    ctx.Response.StatusCode = 402;
                    ctx.Response.ContentType = "application/json";
                    await ctx.Response.WriteAsync(JsonSerializer.Serialize(error));
                    return;
                }
            }
        }

        await next(ctx);
    }

    private async Task<object?> CheckLimitsAsync(string path, string tier, Guid userId, AppDbContext db)
    {
        var planLimits = limits.Get(tier);

        // Check campaign creation limit
        if (path.EndsWith("/api/campaigns") && !path.Contains("/members") && !path.Contains("/invitations"))
        {
            if (planLimits.MaxCampaigns < 0) return null;
            var count = await db.Campaigns.CountAsync(c => c.OwnerId == userId);
            if (count >= planLimits.MaxCampaigns)
                return new { error = "limit_reached", limit = "campaigns", current = count, max = planLimits.MaxCampaigns, requiredTier = NextTier(tier) };
        }

        // Check entities per campaign
        var entityMatch = System.Text.RegularExpressions.Regex.Match(
            path, @"/api/campaigns/([0-9a-f-]+)/entities$", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        if (entityMatch.Success && Guid.TryParse(entityMatch.Groups[1].Value, out var campaignId))
        {
            if (planLimits.MaxEntitiesPerCampaign < 0) return null;
            var count = await db.WorldEntities.CountAsync(e => e.CampaignId == campaignId);
            if (count >= planLimits.MaxEntitiesPerCampaign)
                return new { error = "limit_reached", limit = "entitiesPerCampaign", current = count, max = planLimits.MaxEntitiesPerCampaign, requiredTier = NextTier(tier) };
        }

        return null;
    }

    private static Guid? GetUserId(HttpContext ctx)
    {
        var claim = ctx.User.FindFirstValue(ClaimTypes.NameIdentifier);
        return claim is not null && Guid.TryParse(claim, out var id) ? id : null;
    }

    private static string? GetTier(HttpContext ctx) =>
        ctx.User.FindFirstValue("subscription_tier");

    private static string NextTier(string tier) => tier switch
    {
        "free" => "pro",
        "pro"  => "master",
        _      => "master"
    };
}

public class TierLimits
{
    public int MaxCampaigns { get; init; }
    public int MaxEntitiesPerCampaign { get; init; }
    public int MaxPlayersPerCampaign { get; init; }
    public int MaxCustomEntityTypes { get; init; }
}

public class PlanLimits
{
    public TierLimits Free    { get; init; } = new() { MaxCampaigns = 1,  MaxEntitiesPerCampaign = 20,  MaxPlayersPerCampaign = 4,  MaxCustomEntityTypes = 0 };
    public TierLimits Pro     { get; init; } = new() { MaxCampaigns = 5,  MaxEntitiesPerCampaign = 200, MaxPlayersPerCampaign = 8,  MaxCustomEntityTypes = 5 };
    public TierLimits Master  { get; init; } = new() { MaxCampaigns = -1, MaxEntitiesPerCampaign = -1,  MaxPlayersPerCampaign = -1, MaxCustomEntityTypes = -1 };

    public TierLimits Get(string tier) => tier switch
    {
        SubscriptionTiers.Pro    => Pro,
        SubscriptionTiers.Master => Master,
        _                        => Free,
    };
}
