namespace PropFlow.Domain.Channels;

/// <summary>
/// Contract implemented by each OTA channel adapter.
/// Adapters are pure HTTP clients — no domain logic.
/// All methods are idempotent: safe to retry on transient failures.
/// </summary>
public interface IChannelAdapter
{
    /// <summary>Stable lowercase code matching ChannelConnection.ChannelCode.</summary>
    string ChannelCode { get; }

    /// <summary>
    /// Push ARI availability update for a single (RoomTypeCode, Date).
    /// Called after every InventoryUpdated domain event.
    /// </summary>
    Task PushAvailabilityAsync(
        EncryptedCredentials credentials,
        AvailabilityUpdate update,
        CancellationToken ct = default);

    /// <summary>
    /// Push ARI rate update for a single (RatePlanCode, RoomTypeCode, Date).
    /// Called after every RatePlanPublished domain event.
    /// Distinct from availability — OTAs require both messages separately.
    /// </summary>
    Task PushRateAsync(
        EncryptedCredentials credentials,
        RateUpdate update,
        CancellationToken ct = default);

    /// <summary>
    /// Push ARI restriction update (MinStay, MaxStay, CtA, CtD) for a (RatePlanCode, Date).
    /// </summary>
    Task PushRestrictionAsync(
        EncryptedCredentials credentials,
        RestrictionUpdate update,
        CancellationToken ct = default);

    /// <summary>
    /// Retrieve new reservations created on the OTA since the last pull.
    /// Used as a fallback for channels that push via webhook AND for reconciliation.
    /// </summary>
    Task<IReadOnlyList<InboundReservation>> PullReservationsAsync(
        EncryptedCredentials credentials,
        DateTime since,
        CancellationToken ct = default);

    /// <summary>
    /// Acknowledge a reservation retrieved via PullReservations.
    /// Prevents the OTA from re-delivering it on the next pull.
    /// </summary>
    Task AcknowledgeReservationAsync(
        EncryptedCredentials credentials,
        string channelBookingRef,
        CancellationToken ct = default);
}

// ─── Value objects for adapter I/O ─────────────────────────────────────────────────

public sealed record AvailabilityUpdate(
    string HotelCode,
    string RoomTypeCode,
    DateOnly Date,
    int Available);

public sealed record RateUpdate(
    string HotelCode,
    string RoomTypeCode,
    string RatePlanCode,
    DateOnly Date,
    decimal BaseRate,
    decimal? ExtraAdultRate,
    decimal? ExtraChildRate,
    string Currency);

public sealed record RestrictionUpdate(
    string HotelCode,
    string RatePlanCode,
    DateOnly Date,
    int? MinStay,
    int? MaxStay,
    bool ClosedToArrival,
    bool ClosedToDeparture);

/// <summary>Inbound reservation received from an OTA pull or webhook.</summary>
public sealed record InboundReservation(
    string ChannelBookingRef,
    string RoomTypeCode,
    string RatePlanCode,
    DateOnly CheckInDate,
    DateOnly CheckOutDate,
    int Adults,
    int Children,
    decimal TotalAmount,
    string Currency,
    InboundGuest Guest,
    string? GuaranteeToken,
    ReservationAction Action);

public sealed record InboundGuest(
    string FirstName,
    string LastName,
    string? Email,
    string? Phone,
    string? Nationality);

/// <summary>New = create booking, Modified = update, Cancelled = cancel.</summary>
public enum ReservationAction { New, Modified, Cancelled }
