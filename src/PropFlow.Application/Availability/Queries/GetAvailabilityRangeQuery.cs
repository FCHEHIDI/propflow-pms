using MediatR;
using PropFlow.Application.ReadModels;

namespace PropFlow.Application.Availability.Queries;

/// <summary>
/// Returns AvailabilityView per day for (PropertyId, RoomTypeId) over [From, To).
/// Handler lives in Infrastructure (requires IQuerySession from Marten).
/// </summary>
public sealed record GetAvailabilityRangeQuery(
    Guid PropertyId,
    Guid RoomTypeId,
    DateOnly From,
    DateOnly To) : IRequest<IReadOnlyList<AvailabilityView>>;
