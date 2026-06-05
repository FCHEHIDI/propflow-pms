namespace PropFlow.Domain.Bookings;

public interface IBookingRepository
{
    Task<Booking?> FindByIdAsync(Guid id, CancellationToken ct = default);
    Task<Booking?> FindByChannelRefAsync(string channelCode, string channelBookingRef, CancellationToken ct = default);
    Task<IReadOnlyList<Booking>> FindActiveByRoomTypeAndDateRangeAsync(
        Guid roomTypeId, DateOnly from, DateOnly to, CancellationToken ct = default);
    Task SaveAsync(Booking booking, CancellationToken ct = default);
    Task UpdateAsync(Booking booking, CancellationToken ct = default);
}
