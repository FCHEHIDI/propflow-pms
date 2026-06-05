using MassTransit;
using Marten;
using PropFlow.Domain.Inventory;

namespace PropFlow.Infrastructure.Persistence.Repositories;

public sealed class PgAllotmentRepository : IAllotmentRepository
{
    private readonly IDocumentSession _session;
    private readonly IPublishEndpoint _bus;

    public PgAllotmentRepository(IDocumentSession session, IPublishEndpoint bus)
    {
        _session = session;
        _bus = bus;
    }

    public async Task<Allotment?> FindAsync(
        Guid propertyId, Guid roomTypeId, DateOnly date, CancellationToken ct = default) =>
        await _session.Query<Allotment>()
            .Where(a => a.PropertyId == propertyId
                && a.RoomTypeId == roomTypeId
                && a.Date == date)
            .FirstOrDefaultAsync(ct);

    public async Task<IReadOnlyList<Allotment>> FindRangeAsync(
        Guid propertyId, Guid roomTypeId, DateOnly from, DateOnly to, CancellationToken ct = default) =>
        await _session.Query<Allotment>()
            .Where(a => a.PropertyId == propertyId
                && a.RoomTypeId == roomTypeId
                && a.Date >= from
                && a.Date < to)
            .OrderBy(a => a.Date)
            .ToListAsync(ct);

    public async Task SaveAsync(Allotment allotment, CancellationToken ct = default)
    {
        _session.Store(allotment);
        await _session.SaveChangesAsync(ct);
        await DispatchAsync(allotment, ct);
    }

    public async Task UpdateAsync(Allotment allotment, CancellationToken ct = default)
    {
        _session.Store(allotment);
        await _session.SaveChangesAsync(ct);
        await DispatchAsync(allotment, ct);
    }

    public async Task SaveBatchAsync(IEnumerable<Allotment> allotments, CancellationToken ct = default)
    {
        var batch = allotments.ToList();
        foreach (var a in batch)
            _session.Store(a);
        await _session.SaveChangesAsync(ct);
        foreach (var a in batch)
            await DispatchAsync(a, ct);
    }

    private async Task DispatchAsync(Allotment allotment, CancellationToken ct)
    {
        foreach (var @event in allotment.DomainEvents)
            await _bus.Publish(@event, ct);
        allotment.ClearDomainEvents();
    }
}
