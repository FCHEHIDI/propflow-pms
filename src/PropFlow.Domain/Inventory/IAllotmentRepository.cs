namespace PropFlow.Domain.Inventory;

public interface IAllotmentRepository
{
    Task<Allotment?> FindAsync(
        Guid propertyId, Guid roomTypeId, DateOnly date, CancellationToken ct = default);
    Task<IReadOnlyList<Allotment>> FindRangeAsync(
        Guid propertyId, Guid roomTypeId, DateOnly from, DateOnly to, CancellationToken ct = default);
    Task SaveAsync(Allotment allotment, CancellationToken ct = default);
    Task UpdateAsync(Allotment allotment, CancellationToken ct = default);
    Task SaveBatchAsync(IEnumerable<Allotment> allotments, CancellationToken ct = default);
}
