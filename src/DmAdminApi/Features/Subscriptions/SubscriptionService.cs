using DmAdminApi.Infrastructure.Data;
using DmAdminApi.Infrastructure.Data.Entities;
using DmAdminApi.Infrastructure.Stripe;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Stripe;

// Aliases to avoid conflict with our class name and between Checkout/BillingPortal
using CheckoutSessionService = Stripe.Checkout.SessionService;
using CheckoutSessionCreateOptions = Stripe.Checkout.SessionCreateOptions;
using CheckoutSessionLineItemOptions = Stripe.Checkout.SessionLineItemOptions;
using PortalSessionService = Stripe.BillingPortal.SessionService;
using PortalSessionCreateOptions = Stripe.BillingPortal.SessionCreateOptions;
using StripeSubService = Stripe.SubscriptionService;

namespace DmAdminApi.Features.Subscriptions;

public class SubscriptionService(
    AppDbContext db,
    IOptions<StripeSettings> stripeOpts,
    ILogger<SubscriptionService> logger)
{
    private readonly StripeSettings _stripe = stripeOpts.Value;

    public async Task<string> CreateCheckoutSessionAsync(
        Guid userId, string tier, string successUrl, string cancelUrl)
    {
        var priceId = tier switch
        {
            SubscriptionTiers.Pro => _stripe.ProPriceId,
            SubscriptionTiers.Master => _stripe.MasterPriceId,
            _ => throw new ArgumentException($"Invalid tier: {tier}")
        };

        var user = await db.Users.FindAsync(userId)
            ?? throw new InvalidOperationException("User not found");

        var customerId = await EnsureStripeCustomerAsync(user);

        var session = await new CheckoutSessionService().CreateAsync(new CheckoutSessionCreateOptions
        {
            Customer = customerId,
            ClientReferenceId = userId.ToString(),
            Mode = "subscription",
            LineItems = [new CheckoutSessionLineItemOptions { Price = priceId, Quantity = 1 }],
            SuccessUrl = successUrl,
            CancelUrl = cancelUrl,
        });

        return session.Url;
    }

    public async Task<string> CreatePortalSessionAsync(Guid userId, string returnUrl)
    {
        var user = await db.Users.FindAsync(userId)
            ?? throw new InvalidOperationException("User not found");

        if (string.IsNullOrEmpty(user.StripeCustomerId))
            throw new InvalidOperationException("No Stripe customer found for this user");

        var session = await new PortalSessionService().CreateAsync(new PortalSessionCreateOptions
        {
            Customer = user.StripeCustomerId,
            ReturnUrl = returnUrl,
        });

        return session.Url;
    }

    public async Task HandleWebhookAsync(string payload, string signature)
    {
        var webhookEvent = EventUtility.ConstructEvent(payload, signature, _stripe.WebhookSecret);

        switch (webhookEvent.Type)
        {
            case "checkout.session.completed":
                await HandleCheckoutCompletedAsync((Stripe.Checkout.Session)webhookEvent.Data.Object);
                break;
            case "customer.subscription.deleted":
                await HandleSubscriptionDeletedAsync((Stripe.Subscription)webhookEvent.Data.Object);
                break;
            case "customer.subscription.updated":
                await HandleSubscriptionUpdatedAsync((Stripe.Subscription)webhookEvent.Data.Object);
                break;
        }
    }

    private async Task HandleCheckoutCompletedAsync(Stripe.Checkout.Session session)
    {
        if (!Guid.TryParse(session.ClientReferenceId, out var userId)) return;
        var user = await db.Users.FindAsync(userId);
        if (user == null) return;

        if (!string.IsNullOrEmpty(session.CustomerId))
            user.StripeCustomerId = session.CustomerId;

        if (!string.IsNullOrEmpty(session.SubscriptionId))
        {
            var sub = await new StripeSubService().GetAsync(session.SubscriptionId);
            user.SubscriptionTier = GetTierFromPriceId(sub.Items.Data.FirstOrDefault()?.Price.Id);
        }

        await db.SaveChangesAsync();
        logger.LogInformation("User {UserId} upgraded to {Tier}", userId, user.SubscriptionTier);
    }

    private async Task HandleSubscriptionDeletedAsync(Stripe.Subscription subscription)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.StripeCustomerId == subscription.CustomerId);
        if (user == null) return;

        user.SubscriptionTier = SubscriptionTiers.Free;
        await db.SaveChangesAsync();
        logger.LogInformation("User {UserId} subscription deleted, downgraded to free", user.Id);
    }

    private async Task HandleSubscriptionUpdatedAsync(Stripe.Subscription subscription)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.StripeCustomerId == subscription.CustomerId);
        if (user == null) return;

        user.SubscriptionTier = subscription.Status is "canceled" or "unpaid"
            ? SubscriptionTiers.Free
            : GetTierFromPriceId(subscription.Items.Data.FirstOrDefault()?.Price.Id);

        await db.SaveChangesAsync();
        logger.LogInformation("User {UserId} subscription updated to {Tier}", user.Id, user.SubscriptionTier);
    }

    private async Task<string> EnsureStripeCustomerAsync(User user)
    {
        if (!string.IsNullOrEmpty(user.StripeCustomerId))
            return user.StripeCustomerId;

        var customer = await new CustomerService().CreateAsync(new CustomerCreateOptions
        {
            Email = user.Email,
            Name = user.DisplayName,
            Metadata = new Dictionary<string, string> { ["userId"] = user.Id.ToString() }
        });

        user.StripeCustomerId = customer.Id;
        await db.SaveChangesAsync();
        return customer.Id;
    }

    private string GetTierFromPriceId(string? priceId) =>
        priceId == _stripe.MasterPriceId ? SubscriptionTiers.Master :
        priceId == _stripe.ProPriceId ? SubscriptionTiers.Pro :
        SubscriptionTiers.Free;
}
