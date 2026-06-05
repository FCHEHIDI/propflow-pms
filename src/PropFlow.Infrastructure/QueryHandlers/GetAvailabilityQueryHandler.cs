using MediatR;
using Marten;
using PropFlow.Application.Availability.Queries;
using PropFlow.Application.ReadModels;

namespace PropFlow.Infrastructure.QueryHandlers;

/// <summary>
/// Reads AvailabilityView documents from Marten for the requested date range.
/// This is the query path for:
/// - Channel manager availability checks before booking
/// - OTA push (complement to InventoryUpdated push)
/// - RevOps dashboards and forecasting
/// </summary>
public sealed class GetAvailabilityRangeQueryHandler
    : IRequestHandler<GetAvailabilityRangeQuery, IReadOnlyList<AvailabilityView>>
{
    private readonly IQuerySession _session;

    public GetAvailabilityRangeQueryHandler(IQuerySession session) => _session = session;

    public async Task<IReadOnlyList<AvailabilityView>> Handle(
        GetAvailabilityRangeQuery query, CancellationToken ct) =>
        await _session.Query<AvailabilityView>()
            .Where(v => v.PropertyId == query.PropertyId
                && v.RoomTypeId == query.RoomTypeId
                && v.Date >= query.From
                && v.Date < query.To)
            .OrderBy(v => v.Date)
            .ToListAsync(ct);
}
