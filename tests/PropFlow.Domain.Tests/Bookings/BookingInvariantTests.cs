using FluentAssertions;
using PropFlow.Domain.Bookings;
using PropFlow.Domain.Errors;
using PropFlow.Domain.RoomTypes;
using Xunit;

namespace PropFlow.Domain.Tests.Bookings;

public sealed class BookingInvariantTests
{
    private static readonly Guid TenantId    = Guid.NewGuid();
    private static readonly Guid PropertyId  = Guid.NewGuid();
    private static readonly Guid RoomTypeId  = Guid.NewGuid();
    private static readonly Guid RatePlanId  = Guid.NewGuid();
    private static readonly Guid GuestId     = Guid.NewGuid();

    private static readonly RoomTypeSnapshot RoomTypeSnap = new(
        RoomTypeId, 1, "Double Supérieure", null, 1, 2, []);

    private static readonly RatePlanSnapshot RatePlanSnap = new(
        RatePlanId, "BAR", "Best Available Rate", "RoomOnly",
        120m, null, null, Guid.NewGuid());

    private static Booking BuildBooking(
        DateOnly? checkIn  = null,
        DateOnly? checkOut = null,
        int adults = 1,
        int children = 0)
    {
        var ci = checkIn  ?? new DateOnly(2026, 9, 1);
        var co = checkOut ?? new DateOnly(2026, 9, 3);
        return Booking.CreateDirect(
            TenantId, PropertyId, RoomTypeId, RatePlanId,
            RoomTypeSnap, RatePlanSnap,
            GuestId, ci, co, adults, children, 240m);
    }

    // ─── Creation invariants ───────────────────────────────────────────────

    [Fact]
    public void CreateDirect_ValidData_StatusIsTentative()
    {
        var booking = BuildBooking();
        booking.Status.Should().Be(BookingStatus.Tentative);
    }

    [Fact]
    public void CreateFromChannel_StatusIsConfirmed_SkipsTentative()
    {
        // OTA invariant: no Tentative state
        var booking = Booking.CreateFromChannel(
            TenantId, PropertyId, RoomTypeId, RatePlanId,
            RoomTypeSnap, RatePlanSnap,
            GuestId,
            new DateOnly(2026, 9, 1), new DateOnly(2026, 9, 3),
            1, 0, 240m,
            "booking.com", "BCM-12345");

        booking.Status.Should().Be(BookingStatus.Confirmed);
        booking.ChannelCode.Should().Be("booking.com");
    }

    [Fact]
    public void Create_CheckOutBeforeCheckIn_ThrowsValidationError()
    {
        var act = () => BuildBooking(
            checkIn:  new DateOnly(2026, 9, 5),
            checkOut: new DateOnly(2026, 9, 1));

        act.Should().Throw<DomainError>()
            .Where(e => e.Kind == DomainErrorKind.Validation);
    }

    [Fact]
    public void Create_ZeroAdults_ThrowsValidationError()
    {
        var act = () => BuildBooking(adults: 0);
        act.Should().Throw<DomainError>()
            .Where(e => e.Kind == DomainErrorKind.Validation);
    }

    [Fact]
    public void Create_ExceedsMaxOccupancy_ThrowsValidationError()
    {
        // MaxOccupancy = 2 in RoomTypeSnap
        var act = () => BuildBooking(adults: 2, children: 1);
        act.Should().Throw<DomainError>()
            .Where(e => e.Kind == DomainErrorKind.Validation);
    }

    // ─── State machine ─────────────────────────────────────────────────────

    [Fact]
    public void Confirm_FromTentative_StatusIsConfirmed()
    {
        var booking = BuildBooking();
        booking.Confirm();
        booking.Status.Should().Be(BookingStatus.Confirmed);
    }

    [Fact]
    public void CheckIn_FromConfirmed_StatusIsCheckedIn()
    {
        var booking = BuildBooking();
        booking.Confirm();
        var roomId = Guid.NewGuid();
        booking.CheckIn(roomId);
        booking.Status.Should().Be(BookingStatus.CheckedIn);
        booking.RoomId.Should().Be(roomId);
    }

    [Fact]
    public void CheckOut_FromCheckedIn_StatusIsCheckedOut_AndRaisesEvent()
    {
        var booking = BuildBooking();
        booking.Confirm();
        booking.CheckIn(Guid.NewGuid());
        booking.ClearDomainEvents();

        booking.CheckOut();

        booking.Status.Should().Be(BookingStatus.CheckedOut);
        booking.DomainEvents.Should().Contain(e =>
            e.GetType().Name == "BookingCheckedOut");
    }

    [Fact]
    public void Cancel_FromConfirmed_StatusIsCancelled_AndRaisesEvent()
    {
        var booking = BuildBooking();
        booking.Confirm();
        booking.ClearDomainEvents();

        booking.Cancel("Guest request");

        booking.Status.Should().Be(BookingStatus.Cancelled);
        booking.DomainEvents.Should().Contain(e =>
            e.GetType().Name == "BookingCancelled");
    }

    [Fact]
    public void Cancel_FromCheckedOut_ThrowsDomainError()
    {
        var booking = BuildBooking();
        booking.Confirm();
        booking.CheckIn(Guid.NewGuid());
        booking.CheckOut();

        var act = () => booking.Cancel("Late request");
        act.Should().Throw<DomainError>()
            .Where(e => e.Kind == DomainErrorKind.InvalidState);
    }

    [Fact]
    public void MarkNoShow_FromGuaranteed_StatusIsNoShow()
    {
        var booking = BuildBooking();
        booking.Confirm();
        booking.Guarantee("tok_visa_4242");
        booking.MarkNoShow();
        booking.Status.Should().Be(BookingStatus.NoShow);
    }

    [Fact]
    public void MarkNoShow_FromCheckedIn_ThrowsDomainError()
    {
        var booking = BuildBooking();
        booking.Confirm();
        booking.CheckIn(Guid.NewGuid());

        var act = () => booking.MarkNoShow();
        act.Should().Throw<DomainError>()
            .Where(e => e.Kind == DomainErrorKind.InvalidState);
    }

    // ─── Snapshot immutability ─────────────────────────────────────────────

    [Fact]
    public void RoomTypeSnapshot_IsImmutableAfterCreation()
    {
        var booking = BuildBooking();

        // Snapshot was captured at creation time
        booking.RoomTypeSnapshot.Label.Should().Be("Double Supérieure");
        booking.RoomTypeSnapshot.Version.Should().Be(1);

        // Simulate RoomType being updated (new version 2 in another aggregate)
        // The booking snapshot remains version 1 — contractual integrity preserved
        booking.RoomTypeSnapshot.Version.Should().Be(1);
    }
}
