using Marten;
using PropFlow.Domain.Inventory;

namespace PropFlow.Infrastructure.Persistence.Repositories;

public sealed class PgAllotmentRepository : IAllotmentRepository
{
    private readonly IDocumentSession _session;

    public PgAllotmentRepository(IDocumentSession session) => _session = session;

    public async Task<Allotment?> FindAsync(
        Guid propertyId, Guid roomTypeId, DateOnly date, CancellationToken ct = default) =>
        await _session.Query<Allotment>()
            .Where(a => a.PropertyId == propertyId && a.RoomTypeId == roomTypeId && a.Date == date)
            .FirstOrDefaultAsync(ct);

    public async Task<IReadOnlyList<Allotment>> FindRangeAsync(
        Guid propertyId, Guid roomTypeId, DateOnly from, DateOnly to, CancellationToken ct = default) =>
        await _session.Query<Allotment>()
            .Where(a => a.PropertyId == propertyId && a.RoomTypeId == roomTypeId
                && a.Date >= from && a.Date < to)
            .OrderBy(a => a.Date)
            .ToListAsync(ct);

    public async Task SaveAsync(Allotment allotment, CancellationToken ct = default)
    {
        _session.Store(allotment);
        await _session.SaveChangesAsync(ct);
        allotment.ClearDomainEvents();
    }

    public async Task UpdateAsync(Allotment allotment, CancellationToken ct = default)
    {
        _session.Store(allotment);
        await _session.SaveChangesAsync(ct);
        allotment.ClearDomainEvents();
    }

    public async Task SaveBatchAsync(IEnumerable<Allotment> allotments, CancellationToken ct = default)
    {
        foreach (var a in allotments)
            _session.Store(a);
        await _session.SaveChangesAsync(ct);
    }
}
