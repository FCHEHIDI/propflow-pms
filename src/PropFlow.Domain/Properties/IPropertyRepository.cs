namespace PropFlow.Domain.Properties;

public interface IPropertyRepository
{
    Task<Property?> FindByIdAsync(Guid id, CancellationToken ct = default);
    Task<Property?> FindByTenantAsync(Guid tenantId, CancellationToken ct = default);
    Task SaveAsync(Property property, CancellationToken ct = default);
    Task UpdateAsync(Property property, CancellationToken ct = default);
}
