using PropFlow.Domain.Rooms;

namespace PropFlow.Domain.Events;

// ─── Room ─────────────────────────────────────────────────────────────────────

/// <summary>Consumed by HousekeepingProjection and IoTPanelService.</summary>
public sealed record RoomStatusChanged(
    Guid RoomId,
    Guid PropertyId,
    RoomStatus OldStatus,
    RoomStatus NewStatus)
{
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}

/// <summary>Triggers Allotment.RemoveRoom() for all future dates.</summary>
public sealed record RoomRemovedFromInventory(Guid RoomId, Guid PropertyId, Guid RoomTypeId)
{
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}

public sealed record RoomRestoredToInventory(Guid RoomId, Guid PropertyId, Guid RoomTypeId)
{
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}

// ─── RoomType ─────────────────────────────────────────────────────────────────

public sealed record RoomTypePublished(Guid RoomTypeId, Guid PropertyId)
{
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}

public sealed record RoomTypeSuspended(Guid RoomTypeId, Guid PropertyId)
{
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}

public sealed record RoomTypeDeprecated(Guid RoomTypeId, Guid PropertyId)
{
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}

// ─── RatePlan ─────────────────────────────────────────────────────────────────

/// <summary>If IsPublic=true, triggers channel sync.</summary>
public sealed record RatePlanPublished(Guid RatePlanId, Guid PropertyId, bool IsPublic)
{
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}

public sealed record RatePlanArchived(Guid RatePlanId, Guid PropertyId)
{
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}

// ─── Booking ──────────────────────────────────────────────────────────────────

public sealed record BookingCreated(
    Guid BookingId,
    Guid PropertyId,
    Guid RoomTypeId,
    DateOnly CheckInDate,
    DateOnly CheckOutDate,
    string Source)
{
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}

/// <summary>Triggers Allotment.Decrement() per night in [CheckIn, CheckOut).</summary>
public sealed record BookingConfirmed(
    Guid BookingId,
    Guid PropertyId,
    Guid RoomTypeId,
    DateOnly CheckInDate,
    DateOnly CheckOutDate)
{
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}

/// <summary>Triggers Allotment.Increment() — releases inventory back to channels.</summary>
public sealed record BookingCancelled(
    Guid BookingId,
    Guid PropertyId,
    Guid RoomTypeId,
    DateOnly CheckInDate,
    DateOnly CheckOutDate,
    string Reason,
    string PreviousStatus)
{
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}

/// <summary>Triggers Room.Occupy(OccupancyKind).</summary>
public sealed record BookingCheckedIn(Guid BookingId, Guid PropertyId, Guid RoomId)
{
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}

/// <summary>Triggers Room.Vacate() → VacantDirty → housekeeping signal chain.</summary>
public sealed record BookingCheckedOut(Guid BookingId, Guid PropertyId, Guid RoomId)
{
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}

public sealed record BookingNoShow(Guid BookingId, Guid PropertyId, Guid GuestId)
{
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}

// ─── Inventory ────────────────────────────────────────────────────────────────

/// <summary>Primary signal consumed by ChannelSyncService to push ARI updates to OTAs.</summary>
public sealed record InventoryUpdated(
    Guid PropertyId,
    Guid RoomTypeId,
    DateOnly Date,
    int Available)
{
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}

// ─── Channel ──────────────────────────────────────────────────────────────────

public sealed record ChannelConnected(Guid ConnectionId, Guid PropertyId, string ChannelCode)
{
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}

public sealed record ChannelDisconnected(Guid ConnectionId, Guid PropertyId, string ChannelCode)
{
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}

public sealed record ChannelSyncFailed(
    Guid ConnectionId,
    Guid PropertyId,
    string ChannelCode,
    string ErrorMessage)
{
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}
