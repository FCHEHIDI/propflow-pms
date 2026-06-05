using MassTransit;
using Marten;
using PropFlow.Application.ReadModels;
using PropFlow.Domain.Events;
using PropFlow.Domain.Rooms;

namespace PropFlow.Infrastructure.Projections;

/// <summary>
/// Maintains the RoomStatusBoardView read model consumed by:
/// - Housekeeping tablets (live board)
/// - IoTPanelService (door panel signals)
/// - Front-desk dashboard
///
/// Handles RoomCreated (creates initial document) and RoomStatusChanged (updates status).
/// </summary>
public sealed class RoomStatusBoardProjection :
    IConsumer<RoomCreated>,
    IConsumer<RoomStatusChanged>
{
    private readonly IDocumentSession _session;

    public RoomStatusBoardProjection(IDocumentSession session) => _session = session;

    public async Task Consume(ConsumeContext<RoomCreated> context)
    {
        var e = context.Message;
        var view = new RoomStatusBoardView
        {
            Id = e.RoomId,
            PropertyId = e.PropertyId,
            RoomNumber = e.RoomNumber,
            Floor = e.Floor,
            Wing = e.Wing,
            Building = e.Building,
            Status = RoomStatus.Available,
            LastChangedAt = e.OccurredAt,
        };

        _session.Store(view);
        await _session.SaveChangesAsync(context.CancellationToken);
    }

    public async Task Consume(ConsumeContext<RoomStatusChanged> context)
    {
        var e = context.Message;
        var view = await _session.LoadAsync<RoomStatusBoardView>(e.RoomId, context.CancellationToken);
        if (view is null) return;

        view.Status = e.NewStatus;
        view.LastChangedAt = e.OccurredAt;

        // Clear occupancy data when room is no longer occupied
        if (e.NewStatus != RoomStatus.Occupied)
        {
            view.OccupancyKind = null;
            view.GuestName = null;
            view.CheckOutDate = null;
        }

        _session.Store(view);
        await _session.SaveChangesAsync(context.CancellationToken);
    }
}
