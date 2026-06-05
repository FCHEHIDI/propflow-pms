using MediatR;
using PropFlow.Domain.Bookings;
using PropFlow.Domain.Errors;
using PropFlow.Domain.Rooms;

namespace PropFlow.Application.Bookings.Commands;

public sealed record CheckInCommand(Guid BookingId, Guid RoomId) : IRequest;

public sealed class CheckInCommandHandler : IRequestHandler<CheckInCommand>
{
    private readonly IBookingRepository _bookings;
    private readonly IRoomRepository _rooms;

    public CheckInCommandHandler(IBookingRepository bookings, IRoomRepository rooms)
    {
        _bookings = bookings;
        _rooms = rooms;
    }

    public async Task Handle(CheckInCommand cmd, CancellationToken ct)
    {
        var booking = await _bookings.FindByIdAsync(cmd.BookingId, ct)
            ?? throw DomainError.NotFound($"Booking {cmd.BookingId} not found.");

        var room = await _rooms.FindByIdAsync(cmd.RoomId, ct)
            ?? throw DomainError.NotFound($"Room {cmd.RoomId} not found.");

        // Invariant: assigned room must match the booked RoomType
        if (room.RoomTypeId != booking.RoomTypeId)
            throw DomainError.Conflict(
                $"Room {cmd.RoomId} belongs to RoomType {room.RoomTypeId}, booking requires {booking.RoomTypeId}.");

        // Determine OccupancyKind from booking dates
        var kind = booking.CheckInDate == booking.CheckOutDate
            ? OccupancyKind.DayUse
            : OccupancyKind.Overnight;

        booking.CheckIn(cmd.RoomId);
        room.Occupy(kind);

        await _bookings.UpdateAsync(booking, ct);
        await _rooms.UpdateAsync(room, ct);
    }
}

public sealed record CheckOutCommand(Guid BookingId) : IRequest;

public sealed class CheckOutCommandHandler : IRequestHandler<CheckOutCommand>
{
    private readonly IBookingRepository _bookings;
    private readonly IRoomRepository _rooms;

    public CheckOutCommandHandler(IBookingRepository bookings, IRoomRepository rooms)
    {
        _bookings = bookings;
        _rooms = rooms;
    }

    public async Task Handle(CheckOutCommand cmd, CancellationToken ct)
    {
        var booking = await _bookings.FindByIdAsync(cmd.BookingId, ct)
            ?? throw DomainError.NotFound($"Booking {cmd.BookingId} not found.");

        booking.CheckOut();
        await _bookings.UpdateAsync(booking, ct);

        // BookingCheckedOut event triggers Room.Vacate() via consumer
    }
}

public sealed record CancelBookingCommand(Guid BookingId, string Reason) : IRequest;

public sealed class CancelBookingCommandHandler : IRequestHandler<CancelBookingCommand>
{
    private readonly IBookingRepository _bookings;

    public CancelBookingCommandHandler(IBookingRepository bookings) => _bookings = bookings;

    public async Task Handle(CancelBookingCommand cmd, CancellationToken ct)
    {
        var booking = await _bookings.FindByIdAsync(cmd.BookingId, ct)
            ?? throw DomainError.NotFound($"Booking {cmd.BookingId} not found.");

        booking.Cancel(cmd.Reason);
        await _bookings.UpdateAsync(booking, ct);

        // BookingCancelled event triggers Allotment.Increment() via saga
    }
}
