namespace PropFlow.Application.ReadModels;

/// <summary>
/// Marten document. Updated by AvailabilityViewProjection on every InventoryUpdated event.
/// One document per (PropertyId × RoomTypeId × Date).
/// Id is a deterministic Guid derived from the composite key.
/// </summary>
public sealed class AvailabilityView
{
    public Guid Id { get; set; }
    public Guid PropertyId { get; set; }
    public Guid RoomTypeId { get; set; }
    public string RoomTypeLabel { get; set; } = string.Empty;
    public DateOnly Date { get; set; }
    public int Available { get; set; }
    public decimal? LowestRate { get; set; }
    public DateTime LastUpdatedAt { get; set; }

    /// <summary>Deterministic Guid from (PropertyId:RoomTypeId:Date).</summary>
    public static Guid ComputeId(Guid propertyId, Guid roomTypeId, DateOnly date)
    {
        var key = $"{propertyId}:{roomTypeId}:{date:yyyy-MM-dd}";
        var bytes = System.Text.Encoding.UTF8.GetBytes(key);
        using var sha = System.Security.Cryptography.SHA256.Create();
        var hash = sha.ComputeHash(bytes);
        return new Guid(hash[..16]);
    }
}

public sealed record AvailabilityRangeView(
    Guid PropertyId,
    Guid RoomTypeId,
    string RoomTypeLabel,
    IReadOnlyList<DailyAvailability> Days);

public sealed record DailyAvailability(DateOnly Date, int Available, decimal? LowestRate);
