namespace PropFlow.Application.Tenants;

public interface ITenantProvisioningService
{
    /// <summary>
    /// Creates the Marten schema for a new tenant identifier.
    /// Idempotent — safe to call more than once.
    /// </summary>
    Task ProvisionAsync(string tenantIdentifier, string tenantName, CancellationToken ct = default);
}
