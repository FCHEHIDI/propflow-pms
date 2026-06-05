namespace PropFlow.Domain.Tenants;

public sealed record TenantProvisioned(
    Guid TenantId, string Identifier, string Name, string ContactEmail)
{
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}

public sealed record TenantActivated(Guid TenantId, string StripeCustomerId)
{
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}

public sealed record TenantSuspended(Guid TenantId, string Reason)
{
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}

public sealed record TenantTerminated(Guid TenantId)
{
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}
