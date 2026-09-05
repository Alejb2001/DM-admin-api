namespace DmAdminApi.Features.Subscriptions.Dtos;

public record CheckoutRequest(string Tier, string SuccessUrl, string CancelUrl);
public record PortalRequest(string ReturnUrl);
public record SessionUrlResponse(string Url);
