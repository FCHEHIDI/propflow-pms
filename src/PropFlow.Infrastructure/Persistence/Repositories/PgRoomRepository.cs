using MassTransit;
using Marten;
using PropFlow.Domain.Rooms;

namespace PropFlow.Infrastructure.Persistence.Repositories;

public sealed class PgRoomRepository : IRoomRepository
{
    private readonly IDocumentSession _session;
    private readonly IPublishEndpoint _bus;

    public PgRoomRepository(IDocumentSession session, IPublishEndpoint bus)
    {
        _session = session;
        _bus = bus;
    }

    public async Task<Room?> FindByIdAsync(Guid id, CancellationToken ct = default) =>
        await _session.LoadAsync<Room>(id, ct);

    public async Task<IReadOnlyList<Room>> FindByPropertyAsync(
        Guid propertyId, CancellationToken ct = default) =>
        await _session.Query<Room>()
            .Where(r => r.PropertyId == propertyId)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<Room>> FindByRoomTypeAsync(
        Guid roomTypeId, CancellationToken ct = default) =>
        await _session.Query<Room>()
            .Where(r => r.RoomTypeId == roomTypeId)
            .ToListAsync(ct);

    public async Task<bool> ExistsRoomNumberAsync(
        Guid propertyId, string roomNumber, CancellationToken ct = default) =>
        await _session.Query<Room>()
            .AnyAsync(r => r.PropertyId == propertyId && r.RoomNumber == roomNumber, ct);

    public async Task SaveAsync(Room room, CancellationToken ct = default)
    {
        _session.Store(room);
        await _session.SaveChangesAsync(ct);
        await DispatchAsync(room, ct);
    }

    public async Task UpdateAsync(Room room, CancellationToken ct = default)
    {
        _session.Store(room);
        await _session.SaveChangesAsync(ct);
        await DispatchAsync(room, ct);
    }

    private async Task DispatchAsync(Room room, CancellationToken ct)
    {
        foreach (var @event in room.DomainEvents)
            await _bus.Publish(@event, ct);
        room.ClearDomainEvents();
    }
}
