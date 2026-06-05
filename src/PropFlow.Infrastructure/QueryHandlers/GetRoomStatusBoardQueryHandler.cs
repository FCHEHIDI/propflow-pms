using MediatR;
using Marten;
using PropFlow.Application.ReadModels;
using PropFlow.Application.Rooms.Queries;

namespace PropFlow.Infrastructure.QueryHandlers;

/// <summary>
/// Reads RoomStatusBoardView documents from Marten for the requested property.
/// Ordered by Floor, then RoomNumber — matches natural reading order on a housekeeping tablet.
/// </summary>
public sealed class GetRoomStatusBoardQueryHandler
    : IRequestHandler<GetRoomStatusBoardQuery, IReadOnlyList<RoomStatusBoardView>>
{
    private readonly IQuerySession _session;

    public GetRoomStatusBoardQueryHandler(IQuerySession session) => _session = session;

    public async Task<IReadOnlyList<RoomStatusBoardView>> Handle(
        GetRoomStatusBoardQuery query, CancellationToken ct) =>
        await _session.Query<RoomStatusBoardView>()
            .Where(v => v.PropertyId == query.PropertyId)
            .OrderBy(v => v.Floor)
            .ThenBy(v => v.RoomNumber)
            .ToListAsync(ct);
}
