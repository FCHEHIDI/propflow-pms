using MassTransit;
using Marten;
using PropFlow.Domain.Bookings;

namespace PropFlow.Infrastructure.Persistence.Repositories;

public sealed class PgBookingRepository : IBookingRepository
{
    private readonly IDocumentSession _session;
    private readonly IPublishEndpoint _bus;

    public PgBookingRepository(IDocumentSession session, IPublishEndpoint bus)
    {
        _session = session;
        _bus = bus;
    }

    public async Task<Booking?> FindByIdAsync(Guid id, CancellationToken ct = default) =>
        await _session.LoadAsync<Booking>(id, ct);

    public async Task<Booking?> FindByChannelRefAsync(
        string channelCode, string channelBookingRef, CancellationToken ct = default) =>
        await _session.Query<Booking>()
            .Where(b => b.ChannelCode == channelCode && b.ChannelBookingRef == channelBookingRef)
            .FirstOrDefaultAsync(ct);

    public async Task<IReadOnlyList<Booking>> FindActiveByRoomTypeAndDateRangeAsync(
        Guid roomTypeId, DateOnly from, DateOnly to, CancellationToken ct = default) =>
        await _session.Query<Booking>()
            .Where(b => b.RoomTypeId == roomTypeId
                && b.CheckInDate < to && b.CheckOutDate > from
                && b.Status != BookingStatus.Cancelled
                && b.Status != BookingStatus.NoShow)
            .ToListAsync(ct);

    public async Task SaveAsync(Booking booking, CancellationToken ct = default)
    {
        _session.Store(booking);
        await _session.SaveChangesAsync(ct);
        await DispatchAsync(booking, ct);
    }

    public async Task UpdateAsync(Booking booking, CancellationToken ct = default)
    {
        _session.Store(booking);
        await _session.SaveChangesAsync(ct);
        await DispatchAsync(booking, ct);
    }

    private async Task DispatchAsync(Booking booking, CancellationToken ct)
    {
        foreach (var @event in booking.DomainEvents)
            await _bus.Publish(@event, ct);
        booking.ClearDomainEvents();
    }
}
