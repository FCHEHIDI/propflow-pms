using Marten;
using PropFlow.Domain.Tenants;

namespace PropFlow.Infrastructure.Tenants;

/// <summary>
/// Stores tenant records in the "_admin" Marten scope (shared Postgres schema).
/// Uses IDocumentStore directly to control the tenant scope explicitly.
/// </summary>
public sealed class PgTenantRepository : ITenantRepository
{
    private const string AdminTenant = "_admin";
    private readonly IDocumentStore _store;

    public PgTenantRepository(IDocumentStore store) => _store = store;

    public async Task<Tenant?> FindByIdentifierAsync(
        string identifier, CancellationToken ct = default)
    {
        await using var session = _store.QuerySession(AdminTenant);
        var doc = await session.Query<TenantDocument>()
            .FirstOrDefaultAsync(t => t.Identifier == identifier, ct);
        return doc?.ToDomain();
    }

    public async Task<Tenant?> FindByIdAsync(
        Guid id, CancellationToken ct = default)
    {
        await using var session = _store.QuerySession(AdminTenant);
        var doc = await session.LoadAsync<TenantDocument>(id, ct);
        return doc?.ToDomain();
    }

    public async Task<Tenant?> FindByStripeCustomerIdAsync(
        string stripeCustomerId, CancellationToken ct = default)
    {
        await using var session = _store.QuerySession(AdminTenant);
        var doc = await session.Query<TenantDocument>()
            .FirstOrDefaultAsync(t => t.StripeCustomerId == stripeCustomerId, ct);
        return doc?.ToDomain();
    }

    public async Task SaveAsync(Tenant tenant, CancellationToken ct = default)
    {
        await using var session = _store.LightweightSession(AdminTenant);
        session.Store(TenantDocument.FromDomain(tenant));
        await session.SaveChangesAsync(ct);
    }
}
