using DmAdminApi.Common.Controllers;
using DmAdminApi.Features.Subscriptions.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Stripe;

namespace DmAdminApi.Features.Subscriptions;

[Route("api/subscriptions")]
public class SubscriptionController(
    SubscriptionService subscriptionService,
    ILogger<SubscriptionController> logger) : ApiControllerBase
{
    [HttpPost("checkout")]
    [Authorize]
    public async Task<IActionResult> CreateCheckout([FromBody] CheckoutRequest request)
    {
        try
        {
            var url = await subscriptionService.CreateCheckoutSessionAsync(
                CurrentUserId, request.Tier, request.SuccessUrl, request.CancelUrl);
            return Ok(new SessionUrlResponse(url));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (StripeException ex)
        {
            logger.LogError(ex, "Stripe error creating checkout session");
            return StatusCode(502, new { error = "Error al conectar con Stripe" });
        }
    }

    [HttpPost("portal")]
    [Authorize]
    public async Task<IActionResult> CreatePortal([FromBody] PortalRequest request)
    {
        try
        {
            var url = await subscriptionService.CreatePortalSessionAsync(CurrentUserId, request.ReturnUrl);
            return Ok(new SessionUrlResponse(url));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (StripeException ex)
        {
            logger.LogError(ex, "Stripe error creating portal session");
            return StatusCode(502, new { error = "Error al conectar con Stripe" });
        }
    }

    [HttpPost("webhook")]
    [AllowAnonymous]
    public async Task<IActionResult> Webhook()
    {
        var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();
        var signature = Request.Headers["Stripe-Signature"].FirstOrDefault() ?? string.Empty;

        try
        {
            await subscriptionService.HandleWebhookAsync(json, signature);
            return Ok();
        }
        catch (StripeException ex)
        {
            logger.LogWarning("Invalid Stripe webhook: {Message}", ex.Message);
            return BadRequest();
        }
    }
}
