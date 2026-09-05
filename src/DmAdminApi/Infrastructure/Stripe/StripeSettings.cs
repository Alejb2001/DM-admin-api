namespace DmAdminApi.Infrastructure.Stripe;

public class StripeSettings
{
    public string SecretKey { get; set; } = string.Empty;
    public string WebhookSecret { get; set; } = string.Empty;
    public string ProPriceId { get; set; } = string.Empty;
    public string MasterPriceId { get; set; } = string.Empty;
}
