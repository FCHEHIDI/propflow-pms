using MassTransit;
using Marten;
using PropFlow.Domain.RatePlans;

namespace PropFlow.Infrastructure.Persistence.Repositories;

public sealed class PgRatePlanRepository : IRatePlanRepository
{
    private readonly IDocumentSession _session;
    private readonly IPublishEndpoint _bus;

    public PgRatePlanRepository(IDocumentSession session, IPublishEndpoint bus)
    {
        _session = session;
        _bus     = bus;
    }

    public async Task<RatePlan?> GetAsync(Guid id, CancellationToken ct = default)
        => await _session.LoadAsync<RatePlan>(id, ct);

    public async Task<IReadOnlyList<RatePlan>> GetByPropertyAsync(
        Guid propertyId, CancellationToken ct = default)
        => await _session.Query<RatePlan>()
            .Where(r => r.PropertyId == propertyId)
            .ToListAsync(ct);

    public async Task SaveAsync(RatePlan plan, CancellationToken ct = default)
    {
        _session.Store(plan);
        await _session.SaveChangesAsync(ct);
        foreach (var e in plan.DomainEvents) await _bus.Publish(e, ct);
        plan.ClearDomainEvents();
    }
}
