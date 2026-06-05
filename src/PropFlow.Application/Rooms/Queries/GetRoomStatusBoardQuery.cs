using MediatR;
using PropFlow.Application.ReadModels;

namespace PropFlow.Application.Rooms.Queries;

/// <summary>
/// Returns the full RoomStatusBoardView for a property, ordered by Floor + RoomNumber.
/// Handler lives in Infrastructure (requires IQuerySession from Marten).
/// </summary>
public sealed record GetRoomStatusBoardQuery(Guid PropertyId)
    : IRequest<IReadOnlyList<RoomStatusBoardView>>;
