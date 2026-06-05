namespace PropFlow.Domain.Tenants;

public interface ITenantRepository
{
    Task<Tenant?> FindByIdentifierAsync(string identifier, CancellationToken ct = default);
    Task<Tenant?> FindByIdAsync(Guid id, CancellationToken ct = default);
    Task<Tenant?> FindByStripeCustomerIdAsync(string stripeCustomerId, CancellationToken ct = default);
    Task SaveAsync(Tenant tenant, CancellationToken ct = default);
}
