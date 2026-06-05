namespace PropFlow.Domain.Guests;

public interface IGuestRepository
{
    Task<Guest?> FindByIdAsync(Guid id, CancellationToken ct = default);
    Task<Guest?> FindByEmailAsync(Guid tenantId, string email, CancellationToken ct = default);
    Task SaveAsync(Guest guest, CancellationToken ct = default);
    Task UpdateAsync(Guest guest, CancellationToken ct = default);
}
