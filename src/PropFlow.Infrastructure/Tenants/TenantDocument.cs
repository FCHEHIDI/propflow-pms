using PropFlow.Domain.Tenants;

namespace PropFlow.Infrastructure.Tenants;

/// <summary>
/// Marten persistence DTO for the Tenant aggregate.
/// Stored in the "_admin" tenant scope — a shared schema separate from hotel schemas.
/// </summary>
public sealed class TenantDocument
{
    public Guid Id { get; set; }
    public string Identifier { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string ContactEmail { get; set; } = default!;
    public string Status { get; set; } = "Trial";
    public string PlanId { get; set; } = "trial";
    public DateTimeOffset CreatedAt { get; set; }
    public string? StripeCustomerId { get; set; }
    public string? StripeSubscriptionId { get; set; }

    public Tenant ToDomain() => Tenant.Reconstitute(
        Id, Identifier, Name, ContactEmail,
        Enum.Parse<TenantStatus>(Status),
        PlanId, CreatedAt,
        StripeCustomerId, StripeSubscriptionId);

    public static TenantDocument FromDomain(Tenant t) => new()
    {
        Id                   = t.Id,
        Identifier           = t.Identifier,
        Name                 = t.Name,
        ContactEmail         = t.ContactEmail,
        Status               = t.Status.ToString(),
        PlanId               = t.PlanId,
        CreatedAt            = t.CreatedAt,
        StripeCustomerId     = t.StripeCustomerId,
        StripeSubscriptionId = t.StripeSubscriptionId,
    };
}
