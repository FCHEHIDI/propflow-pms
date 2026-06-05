namespace PropFlow.Domain.RatePlans;

public interface IRatePlanRepository
{
    Task<RatePlan?> FindByIdAsync(Guid id, CancellationToken ct = default);
    Task<bool> ExistsCodeAsync(Guid propertyId, string code, CancellationToken ct = default);
    Task<IReadOnlyList<RatePlan>> FindPublicActiveByPropertyAsync(Guid propertyId, CancellationToken ct = default);
    Task SaveAsync(RatePlan ratePlan, CancellationToken ct = default);
    Task UpdateAsync(RatePlan ratePlan, CancellationToken ct = default);
}
