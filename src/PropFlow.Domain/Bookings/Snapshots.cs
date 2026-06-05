namespace PropFlow.Domain.Bookings;

/// <summary>
/// Immutable copy of RoomType at booking creation time.
/// Invariant: never mutated after Booking is created.
/// Ensures contractual integrity — the guest sees exactly what was sold.
/// </summary>
public sealed record RoomTypeSnapshot(
    Guid RoomTypeId,
    int Version,
    string Label,
    string? Description,
    int BaseOccupancy,
    int MaxOccupancy,
    IReadOnlyList<Guid> AmenityIds);

/// <summary>
/// Immutable copy of RatePlan at booking creation time.
/// </summary>
public sealed record RatePlanSnapshot(
    Guid RatePlanId,
    string Code,
    string Name,
    string MealPlan,
    decimal BaseRate,
    decimal? ExtraAdultRate,
    decimal? ExtraChildRate,
    Guid CancellationPolicyId);
