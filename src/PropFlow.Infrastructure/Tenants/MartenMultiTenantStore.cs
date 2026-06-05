using Finbuckle.MultiTenant;
using Marten;

namespace PropFlow.Infrastructure.Tenants;

/// <summary>
/// Finbuckle IMultiTenantStore backed by Marten.
/// Reads hotel tenant metadata from the "_admin" schema.
/// Replaces the static WithConfigurationStore() (appsettings.json) with a live DB lookup.
/// </summary>
public sealed class MartenMultiTenantStore : IMultiTenantStore<TenantInfo>
{
    private const string AdminTenant = "_admin";
    private readonly IDocumentStore _store;

    public MartenMultiTenantStore(IDocumentStore store) => _store = store;

    public async Task<TenantInfo?> TryGetByIdentifierAsync(string identifier)
    {
        await using var session = _store.QuerySession(AdminTenant);
        var doc = await session.Query<TenantDocument>()
            .FirstOrDefaultAsync(t => t.Identifier == identifier);
        return doc is null ? null : ToTenantInfo(doc);
    }

    public async Task<TenantInfo?> TryGetAsync(string id)
    {
        await using var session = _store.QuerySession(AdminTenant);
        if (!Guid.TryParse(id, out var guid)) return null;
        var doc = await session.LoadAsync<TenantDocument>(guid);
        return doc is null ? null : ToTenantInfo(doc);
    }

    public async Task<IEnumerable<TenantInfo>> GetAllAsync()
    {
        await using var session = _store.QuerySession(AdminTenant);
        var docs = await session.Query<TenantDocument>().ToListAsync();
        return docs.Select(ToTenantInfo);
    }

    public async Task<bool> TryAddAsync(TenantInfo tenantInfo)
    {
        await using var session = _store.LightweightSession(AdminTenant);
        session.Store(FromTenantInfo(tenantInfo));
        await session.SaveChangesAsync();
        return true;
    }

    public async Task<bool> TryUpdateAsync(TenantInfo tenantInfo)
    {
        await using var session = _store.LightweightSession(AdminTenant);
        session.Store(FromTenantInfo(tenantInfo));
        await session.SaveChangesAsync();
        return true;
    }

    public async Task<bool> TryRemoveAsync(string identifier)
    {
        await using var session = _store.LightweightSession(AdminTenant);
        var doc = await session.Query<TenantDocument>()
            .FirstOrDefaultAsync(t => t.Identifier == identifier);
        if (doc is null) return false;
        session.Delete(doc);
        await session.SaveChangesAsync();
        return true;
    }

    private static TenantInfo ToTenantInfo(TenantDocument d) => new()
    {
        Id         = d.Id.ToString(),
        Identifier = d.Identifier,
        Name       = d.Name,
    };

    private static TenantDocument FromTenantInfo(TenantInfo info) => new()
    {
        Id         = Guid.TryParse(info.Id, out var g) ? g : Guid.NewGuid(),
        Identifier = info.Identifier ?? string.Empty,
        Name       = info.Name       ?? string.Empty,
        CreatedAt  = DateTimeOffset.UtcNow,
    };
}
