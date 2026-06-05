using MassTransit;
using MediatR;
using Microsoft.Extensions.Logging;
using PropFlow.Domain.Tenants;

namespace PropFlow.Application.Tenants.Commands;

public enum BillingEvent { SubscriptionActivated, SubscriptionCancelled, PaymentFailed }

public sealed record UpdateTenantBillingCommand(
    string StripeCustomerId,
    string? StripeSubscriptionId,
    BillingEvent Event,
    string? Reason = null) : IRequest;

public sealed class UpdateTenantBillingHandler : IRequestHandler<UpdateTenantBillingCommand>
{
    private readonly ITenantRepository _tenants;
    private readonly IPublishEndpoint _bus;
    private readonly ILogger<UpdateTenantBillingHandler> _logger;

    public UpdateTenantBillingHandler(
        ITenantRepository tenants,
        IPublishEndpoint bus,
        ILogger<UpdateTenantBillingHandler> logger)
    {
        _tenants = tenants;
        _bus     = bus;
        _logger  = logger;
    }

    public async Task Handle(UpdateTenantBillingCommand cmd, CancellationToken ct)
    {
        var tenant = await _tenants.FindByStripeCustomerIdAsync(cmd.StripeCustomerId, ct);
        if (tenant is null)
        {
            _logger.LogWarning(
                "Stripe event {Event} for unknown customer {CustomerId}",
                cmd.Event, cmd.StripeCustomerId);
            return;
        }

        switch (cmd.Event)
        {
            case BillingEvent.SubscriptionActivated:
                tenant.Activate(
                    cmd.StripeCustomerId,
                    cmd.StripeSubscriptionId ?? string.Empty);
                break;

            case BillingEvent.SubscriptionCancelled:
                tenant.Suspend(cmd.Reason ?? "Subscription cancelled");
                break;

            case BillingEvent.PaymentFailed:
                // Grace period — log but don't suspend immediately on first failure.
                _logger.LogWarning(
                    "Payment failed for tenant {Identifier} — grace period active",
                    tenant.Identifier);
                break;
        }

        await _tenants.SaveAsync(tenant, ct);
        foreach (var e in tenant.DomainEvents)
            await _bus.Publish(e, ct);
        tenant.ClearDomainEvents();
    }
}
