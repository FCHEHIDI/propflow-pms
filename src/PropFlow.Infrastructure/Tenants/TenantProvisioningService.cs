using Marten;
using Microsoft.Extensions.Logging;
using PropFlow.Application.Tenants;

namespace PropFlow.Infrastructure.Tenants;

public sealed class TenantProvisioningService : ITenantProvisioningService
{
    private readonly IDocumentStore _store;
    private readonly ILogger<TenantProvisioningService> _logger;

    public TenantProvisioningService(
        IDocumentStore store,
        ILogger<TenantProvisioningService> logger)
    {
        _store  = store;
        _logger = logger;
    }

    public async Task ProvisionAsync(
        string tenantIdentifier, string tenantName, CancellationToken ct = default)
    {
        // Opening a lightweight session for a new tenant triggers Marten's AutoCreate.All
        // to create the schema for that tenant if it doesn't already exist.
        await using var session = _store.LightweightSession(tenantIdentifier);
        await session.SaveChangesAsync(ct);
        _logger.LogInformation(
            "Provisioned Marten schema for tenant '{Identifier}' ({Name})",
            tenantIdentifier, tenantName);
    }
}
