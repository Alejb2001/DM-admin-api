namespace DmAdminApi.Infrastructure.Data.Entities;

public class User
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public string SubscriptionTier { get; set; } = SubscriptionTiers.Free;
    public string? StripeCustomerId { get; set; }
    public DateTime CreatedAt { get; set; }

    public ICollection<RefreshToken> RefreshTokens { get; set; } = [];
}

public static class SubscriptionTiers
{
    public const string Free = "free";
    public const string Pro = "pro";
    public const string Master = "master";
}
