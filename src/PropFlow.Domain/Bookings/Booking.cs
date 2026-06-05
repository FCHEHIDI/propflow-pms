using PropFlow.Domain.Common;
using PropFlow.Domain.Errors;
using PropFlow.Domain.Events;

namespace PropFlow.Domain.Bookings;

public sealed class Booking : AggregateRoot
{
    public Guid PropertyId { get; private set; }
    public Guid RoomTypeId { get; private set; }
    /// <summary>Null until AssignRoom or CheckIn. Room assigned at check-in, not at booking time.</summary>
    public Guid? RoomId { get; private set; }
    public Guid RatePlanId { get; private set; }
    /// <summary>Invariant: immutable after creation. Contractual integrity.</summary>
    public RoomTypeSnapshot RoomTypeSnapshot { get; private set; } = default!;
    /// <summary>Invariant: immutable after creation.</summary>
    public RatePlanSnapshot RatePlanSnapshot { get; private set; } = default!;
    public BookingSource Source { get; private set; }
    public string? ChannelCode { get; private set; }
    public string? ChannelBookingRef { get; private set; }
    public Guid GuestId { get; private set; }
    public DateOnly CheckInDate { get; private set; }
    public DateOnly CheckOutDate { get; private set; }
    public int Adults { get; private set; }
    public int Children { get; private set; }
    public BookingStatus Status { get; private set; }
    public decimal TotalAmount { get; private set; }
    /// <summary>Invariant: never the raw PAN. Tokenised reference only — PCI-DSS compliance.</summary>
    public string? GuaranteeToken { get; private set; }
    public Guid? GroupReservationId { get; private set; }
    public string? Notes { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private Booking() { }

    /// <summary>Creates a direct booking in Tentative state. Awaits guarantee before confirming.</summary>
    public static Booking CreateDirect(
        Guid tenantId,
        Guid propertyId,
        Guid roomTypeId,
        Guid ratePlanId,
        RoomTypeSnapshot roomTypeSnapshot,
        RatePlanSnapshot ratePlanSnapshot,
        Guid guestId,
        DateOnly checkInDate,
        DateOnly checkOutDate,
        int adults,
        int children,
        decimal totalAmount,
        BookingSource source = BookingSource.Direct,
        string? notes = null)
    {
        ValidateStay(checkInDate, checkOutDate, adults, children, roomTypeSnapshot.MaxOccupancy);

        var booking = new Booking
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            PropertyId = propertyId,
            RoomTypeId = roomTypeId,
            RatePlanId = ratePlanId,
            RoomTypeSnapshot = roomTypeSnapshot,
            RatePlanSnapshot = ratePlanSnapshot,
            GuestId = guestId,
            CheckInDate = checkInDate,
            CheckOutDate = checkOutDate,
            Adults = adults,
            Children = children,
            TotalAmount = totalAmount,
            Source = source,
            Status = BookingStatus.Tentative,
            Notes = notes,
            CreatedAt = DateTime.UtcNow,
        };
        booking.Raise(new BookingCreated(
            booking.Id, propertyId, roomTypeId, checkInDate, checkOutDate, source.ToString()));
        return booking;
    }

    /// <summary>
    /// Creates an OTA booking directly in Confirmed state.
    /// OTA bookings skip Tentative — they arrive already confirmed + guaranteed.
    /// </summary>
    public static Booking CreateFromChannel(
        Guid tenantId,
        Guid propertyId,
        Guid roomTypeId,
        Guid ratePlanId,
        RoomTypeSnapshot roomTypeSnapshot,
        RatePlanSnapshot ratePlanSnapshot,
        Guid guestId,
        DateOnly checkInDate,
        DateOnly checkOutDate,
        int adults,
        int children,
        decimal totalAmount,
        string channelCode,
        string channelBookingRef,
        string? guaranteeToken = null)
    {
        ValidateStay(checkInDate, checkOutDate, adults, children, roomTypeSnapshot.MaxOccupancy);

        var booking = new Booking
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            PropertyId = propertyId,
            RoomTypeId = roomTypeId,
            RatePlanId = ratePlanId,
            RoomTypeSnapshot = roomTypeSnapshot,
            RatePlanSnapshot = ratePlanSnapshot,
            GuestId = guestId,
            CheckInDate = checkInDate,
            CheckOutDate = checkOutDate,
            Adults = adults,
            Children = children,
            TotalAmount = totalAmount,
            Source = BookingSource.OTA,
            ChannelCode = channelCode,
            ChannelBookingRef = channelBookingRef,
            GuaranteeToken = guaranteeToken,
            Status = BookingStatus.Confirmed,
            CreatedAt = DateTime.UtcNow,
        };
        booking.Raise(new BookingCreated(
            booking.Id, propertyId, roomTypeId, checkInDate, checkOutDate, BookingSource.OTA.ToString()));
        booking.Raise(new BookingConfirmed(
            booking.Id, propertyId, roomTypeId, checkInDate, checkOutDate));
        return booking;
    }

    public void Confirm()
    {
        if (Status != BookingStatus.Tentative)
            throw DomainError.InvalidState($"Cannot confirm booking in state {Status}.");

        Status = BookingStatus.Confirmed;
        Raise(new BookingConfirmed(Id, PropertyId, RoomTypeId, CheckInDate, CheckOutDate));
    }

    public void Guarantee(string guaranteeToken)
    {
        if (string.IsNullOrWhiteSpace(guaranteeToken))
            throw DomainError.Validation("Guarantee token is required.");
        if (Status != BookingStatus.Confirmed)
            throw DomainError.InvalidState($"Cannot guarantee booking in state {Status}.");

        GuaranteeToken = guaranteeToken;
        Status = BookingStatus.Guaranteed;
    }

    public void AssignRoom(Guid roomId)
    {
        if (Status is not (BookingStatus.Confirmed or BookingStatus.Guaranteed))
            throw DomainError.InvalidState($"Cannot assign room for booking in state {Status}.");
        RoomId = roomId;
    }

    public void CheckIn(Guid roomId)
    {
        if (Status is not (BookingStatus.Confirmed or BookingStatus.Guaranteed))
            throw DomainError.InvalidState($"Cannot check in booking in state {Status}.");

        RoomId = roomId;
        Status = BookingStatus.CheckedIn;
        Raise(new BookingCheckedIn(Id, PropertyId, roomId));
    }

    public void CheckOut()
    {
        if (Status != BookingStatus.CheckedIn)
            throw DomainError.InvalidState($"Cannot check out booking in state {Status}.");

        Status = BookingStatus.CheckedOut;
        Raise(new BookingCheckedOut(Id, PropertyId, RoomId!.Value));
    }

    public void Cancel(string reason)
    {
        if (Status is BookingStatus.CheckedIn or BookingStatus.CheckedOut
            or BookingStatus.NoShow or BookingStatus.Cancelled)
            throw DomainError.InvalidState($"Cannot cancel booking in state {Status}.");

        var previous = Status;
        Status = BookingStatus.Cancelled;
        Raise(new BookingCancelled(
            Id, PropertyId, RoomTypeId, CheckInDate, CheckOutDate, reason, previous.ToString()));
    }

    /// <summary>Triggered by night audit when guest does not arrive. Penalty applies.</summary>
    public void MarkNoShow()
    {
        if (Status is not (BookingStatus.Confirmed or BookingStatus.Guaranteed))
            throw DomainError.InvalidState($"Cannot mark no-show for booking in state {Status}.");

        Status = BookingStatus.NoShow;
        Raise(new BookingNoShow(Id, PropertyId, GuestId));
    }

    public void AssignToGroup(Guid groupReservationId) => GroupReservationId = groupReservationId;

    private static void ValidateStay(
        DateOnly checkIn, DateOnly checkOut, int adults, int children, int maxOccupancy)
    {
        if (checkOut <= checkIn)
            throw DomainError.Validation("CheckOutDate must be strictly after CheckInDate.");
        if (adults < 1)
            throw DomainError.Validation("At least 1 adult is required.");
        if (adults + children > maxOccupancy)
            throw DomainError.Validation(
                $"Total occupants ({adults + children}) exceeds MaxOccupancy ({maxOccupancy}).");
    }
}
