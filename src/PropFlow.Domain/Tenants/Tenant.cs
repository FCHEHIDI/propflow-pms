using PropFlow.Domain.Common;
using PropFlow.Domain.Errors;

namespace PropFlow.Domain.Tenants;

public enum TenantStatus { Trial, Active, Suspended, Terminated }

public sealed class Tenant : AggregateRoot
{
    public Guid Id { get; private set; }
    public string Identifier { get; private set; } = default!;
    public string Name { get; private set; } = default!;
    public string ContactEmail { get; private set; } = default!;
    public TenantStatus Status { get; private set; }
    public string PlanId { get; private set; } = "trial";
    public DateTimeOffset CreatedAt { get; private set; }
    public string? StripeCustomerId { get; private set; }
    public string? StripeSubscriptionId { get; private set; }

    private Tenant() { }

    public static Tenant Create(
        string identifier, string name, string contactEmail, string planId = "trial")
    {
        if (string.IsNullOrWhiteSpace(identifier))
            throw DomainError.Validation("Tenant identifier is required.");
        if (string.IsNullOrWhiteSpace(name))
            throw DomainError.Validation("Tenant name is required.");
        if (string.IsNullOrWhiteSpace(contactEmail))
            throw DomainError.Validation("Contact email is required.");

        var tenant = new Tenant
        {
            Id            = Guid.NewGuid(),
            Identifier    = identifier.ToLowerInvariant().Trim(),
            Name          = name.Trim(),
            ContactEmail  = contactEmail.Trim().ToLowerInvariant(),
            Status        = TenantStatus.Trial,
            PlanId        = planId,
            CreatedAt     = DateTimeOffset.UtcNow,
        };
        tenant.Raise(new TenantProvisioned(
            tenant.Id, tenant.Identifier, tenant.Name, tenant.ContactEmail));
        return tenant;
    }

    /// <summary>Reconstitutes from persistence DTO without raising domain events.</summary>
    internal static Tenant Reconstitute(
        Guid id, string identifier, string name, string contactEmail,
        TenantStatus status, string planId, DateTimeOffset createdAt,
        string? stripeCustomerId, string? stripeSubscriptionId) => new()
    {
        Id                   = id,
        Identifier           = identifier,
        Name                 = name,
        ContactEmail         = contactEmail,
        Status               = status,
        PlanId               = planId,
        CreatedAt            = createdAt,
        StripeCustomerId     = stripeCustomerId,
        StripeSubscriptionId = stripeSubscriptionId,
    };

    public void Activate(string stripeCustomerId, string stripeSubscriptionId)
    {
        if (Status == TenantStatus.Terminated)
            throw DomainError.InvalidState("Cannot activate a terminated tenant.");
        StripeCustomerId     = stripeCustomerId;
        StripeSubscriptionId = stripeSubscriptionId;
        Status = TenantStatus.Active;
        Raise(new TenantActivated(Id, stripeCustomerId));
    }

    public void Suspend(string reason)
    {
        if (Status == TenantStatus.Terminated)
            throw DomainError.InvalidState("Cannot suspend a terminated tenant.");
        Status = TenantStatus.Suspended;
        Raise(new TenantSuspended(Id, reason));
    }

    public void Reactivate()
    {
        if (Status != TenantStatus.Suspended)
            throw DomainError.InvalidState("Only suspended tenants can be reactivated.");
        Status = TenantStatus.Active;
    }

    public void Terminate()
    {
        Status = TenantStatus.Terminated;
        Raise(new TenantTerminated(Id));
    }
}
