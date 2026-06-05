namespace PropFlow.Application.ReadModels;

/// <summary>
/// Per-day availability for a RoomType. Pushed to OTAs on every InventoryUpdated event.
/// Available = TotalRooms - Sold (computed in Allotment aggregate).
/// </summary>
public sealed record AvailabilityView
{
    public Guid PropertyId { get; init; }
    public Guid RoomTypeId { get; init; }
    public string RoomTypeLabel { get; init; } = default!;
    public DateOnly Date { get; init; }
    public int Available { get; init; }
    public decimal? LowestRate { get; init; }
    public DateTime LastUpdatedAt { get; init; }
}

public sealed record AvailabilityRangeView(
    Guid PropertyId,
    Guid RoomTypeId,
    string RoomTypeLabel,
    IReadOnlyList<DailyAvailability> Days);

public sealed record DailyAvailability(DateOnly Date, int Available, decimal? LowestRate);
