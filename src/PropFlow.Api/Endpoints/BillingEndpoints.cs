using MediatR;
using PropFlow.Application.Tenants.Commands;
using Stripe;

namespace PropFlow.Api.Endpoints;

public static class BillingEndpoints
{
    public static RouteGroupBuilder MapBillingEndpoints(this RouteGroupBuilder group)
    {
        // Stripe calls this endpoint — no Bearer token, but signature is verified in-handler.
        group.MapPost("/stripe-webhook", StripeWebhook).AllowAnonymous();
        return group;
    }

    /// <summary>
    /// Verifies the Stripe-Signature header before processing any event.
    /// Raw body must NOT be consumed by model binding — read it manually.
    /// </summary>
    private static async Task<IResult> StripeWebhook(
        HttpRequest request,
        ISender mediator,
        IConfiguration config,
        ILogger<Program> logger,
        CancellationToken ct)
    {
        var webhookSecret = config["Stripe:WebhookSecret"]
            ?? throw new InvalidOperationException("Stripe:WebhookSecret is required.");

        string json;
        using (var reader = new StreamReader(request.Body))
            json = await reader.ReadToEndAsync(ct);

        Event stripeEvent;
        try
        {
            stripeEvent = EventUtility.ConstructEvent(
                json,
                request.Headers["Stripe-Signature"],
                webhookSecret,
                throwOnApiVersionMismatch: false);
        }
        catch (StripeException ex)
        {
            logger.LogWarning(
                "Stripe webhook signature validation failed: {Message}", ex.Message);
            return Results.BadRequest("Invalid signature.");
        }

        switch (stripeEvent.Type)
        {
            case "customer.subscription.created":
            case "customer.subscription.updated":
                if (stripeEvent.Data.Object is Subscription sub && sub.Status == "active")
                    await mediator.Send(
                        new UpdateTenantBillingCommand(
                            sub.CustomerId, sub.Id,
                            BillingEvent.SubscriptionActivated), ct);
                break;

            case "customer.subscription.deleted":
                if (stripeEvent.Data.Object is Subscription deleted)
                    await mediator.Send(
                        new UpdateTenantBillingCommand(
                            deleted.CustomerId, deleted.Id,
                            BillingEvent.SubscriptionCancelled,
                            Reason: "Subscription cancelled"), ct);
                break;

            case "invoice.payment_failed":
                if (stripeEvent.Data.Object is Invoice invoice
                    && invoice.CustomerId is not null)
                    await mediator.Send(
                        new UpdateTenantBillingCommand(
                            invoice.CustomerId, null,
                            BillingEvent.PaymentFailed), ct);
                break;

            default:
                logger.LogDebug("Unhandled Stripe event type: {Type}", stripeEvent.Type);
                break;
        }

        return Results.Ok();
    }
}
