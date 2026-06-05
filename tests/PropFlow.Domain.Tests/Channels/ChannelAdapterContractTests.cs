using FluentAssertions;
using PropFlow.Domain.Channels;
using PropFlow.Domain.Errors;
using PropFlow.Domain.RoomTypes;
using Xunit;

namespace PropFlow.Domain.Tests.Channels;

public sealed class ChannelAdapterContractTests
{
    /// <summary>
    /// Verifies the adapter I/O value objects are well-formed.
    /// Actual HTTP calls are tested in integration tests (require sandbox account).
    /// </summary>

    [Fact]
    public void AvailabilityUpdate_CanBeConstructed()
    {
        var update = new AvailabilityUpdate(
            HotelCode:    "hotel_123",
            RoomTypeCode: "DBL_STD",
            Date:         new DateOnly(2026, 9, 1),
            Available:    5);

        update.Available.Should().Be(5);
        update.Date.ToString("yyyy-MM-dd").Should().Be("2026-09-01");
    }

    [Fact]
    public void RateUpdate_CanBeConstructed()
    {
        var update = new RateUpdate(
            HotelCode:    "hotel_123",
            RoomTypeCode: "DBL_STD",
            RatePlanCode: "BAR",
            Date:         new DateOnly(2026, 9, 1),
            BaseRate:     120m,
            ExtraAdultRate: null,
            ExtraChildRate: null,
            Currency:     "EUR");

        update.BaseRate.Should().Be(120m);
        update.Currency.Should().Be("EUR");
    }

    [Fact]
    public void RoomTypeSnapshot_IsImmutable_RecordEquality()
    {
        var snap1 = new RoomTypeSnapshot(Guid.NewGuid(), 1, "Suite", null, 1, 2, []);
        var snap2 = snap1 with { Label = "Suite Junior" };

        // Original unchanged — records are value-semantic
        snap1.Label.Should().Be("Suite");
        snap2.Label.Should().Be("Suite Junior");
    }

    [Fact]
    public void InboundReservation_Action_ParsedCorrectly()
    {
        var r = new InboundReservation(
            ChannelBookingRef: "BCM-001",
            RoomTypeCode:      "DBL_STD",
            RatePlanCode:      "BAR",
            CheckInDate:       new DateOnly(2026, 9, 1),
            CheckOutDate:      new DateOnly(2026, 9, 3),
            Adults: 2, Children: 0,
            TotalAmount: 240m,
            Currency: "EUR",
            Guest: new InboundGuest("Jean", "Dupont", null, null, "FR"),
            GuaranteeToken: null,
            Action: ReservationAction.New);

        r.Action.Should().Be(ReservationAction.New);
        r.Adults.Should().Be(2);
    }
}
