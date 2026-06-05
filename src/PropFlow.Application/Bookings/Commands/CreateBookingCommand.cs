using MediatR;
using PropFlow.Domain.Bookings;
using PropFlow.Domain.Errors;
using PropFlow.Domain.Guests;
using PropFlow.Domain.Inventory;
using PropFlow.Domain.RatePlans;
using PropFlow.Domain.RoomTypes;

namespace PropFlow.Application.Bookings.Commands;

public sealed record CreateBookingCommand(
    Guid TenantId,
    Guid PropertyId,
    Guid RoomTypeId,
    Guid RatePlanId,
    Guid GuestId,
    DateOnly CheckInDate,
    DateOnly CheckOutDate,
    int Adults,
    int Children,
    BookingSource Source,
    decimal TotalAmount,
    string? ChannelCode = null,
    string? ChannelBookingRef = null,
    string? GuaranteeToken = null,
    string? Notes = null) : IRequest<Guid>;

public sealed class CreateBookingCommandHandler : IRequestHandler<CreateBookingCommand, Guid>
{
    private readonly IBookingRepository _bookings;
    private readonly IRoomTypeRepository _roomTypes;
    private readonly IRatePlanRepository _ratePlans;
    private readonly IAllotmentRepository _allotments;

    public CreateBookingCommandHandler(
        IBookingRepository bookings,
        IRoomTypeRepository roomTypes,
        IRatePlanRepository ratePlans,
        IAllotmentRepository allotments)
    {
        _bookings = bookings;
        _roomTypes = roomTypes;
        _ratePlans = ratePlans;
        _allotments = allotments;
    }

    public async Task<Guid> Handle(CreateBookingCommand cmd, CancellationToken ct)
    {
        var roomType = await _roomTypes.FindByIdAsync(cmd.RoomTypeId, ct)
            ?? throw DomainError.NotFound($"RoomType {cmd.RoomTypeId} not found.");

        if (roomType.Status != RoomTypeStatus.Active)
            throw DomainError.InvalidState($"RoomType must be Active to accept bookings (current: {roomType.Status}).");

        var ratePlan = await _ratePlans.FindByIdAsync(cmd.RatePlanId, ct)
            ?? throw DomainError.NotFound($"RatePlan {cmd.RatePlanId} not found.");

        var roomTypeSnapshot = roomType.TakeSnapshot();
        var ratePlanSnapshot = ratePlan.TakeSnapshot(cmd.RoomTypeId);

        // Verify availability for all nights
        var current = cmd.CheckInDate;
        while (current < cmd.CheckOutDate)
        {
            var allotment = await _allotments.FindAsync(cmd.PropertyId, cmd.RoomTypeId, current, ct)
                ?? throw DomainError.Conflict($"No allotment configured for {cmd.RoomTypeId} on {current}.");

            allotment.Decrement();
            await _allotments.UpdateAsync(allotment, ct);
            current = current.AddDays(1);
        }

        Booking booking;
        if (cmd.Source == BookingSource.OTA && cmd.ChannelCode is not null && cmd.ChannelBookingRef is not null)
        {
            booking = Booking.CreateFromChannel(
                cmd.TenantId, cmd.PropertyId, cmd.RoomTypeId, cmd.RatePlanId,
                roomTypeSnapshot, ratePlanSnapshot,
                cmd.GuestId, cmd.CheckInDate, cmd.CheckOutDate,
                cmd.Adults, cmd.Children, cmd.TotalAmount,
                cmd.ChannelCode, cmd.ChannelBookingRef, cmd.GuaranteeToken);
        }
        else
        {
            booking = Booking.CreateDirect(
                cmd.TenantId, cmd.PropertyId, cmd.RoomTypeId, cmd.RatePlanId,
                roomTypeSnapshot, ratePlanSnapshot,
                cmd.GuestId, cmd.CheckInDate, cmd.CheckOutDate,
                cmd.Adults, cmd.Children, cmd.TotalAmount,
                cmd.Source, cmd.Notes);
        }

        await _bookings.SaveAsync(booking, ct);
        return booking.Id;
    }
}
