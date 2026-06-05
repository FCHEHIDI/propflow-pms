using PropFlow.Domain.Common;
using PropFlow.Domain.Errors;
using PropFlow.Domain.Events;

namespace PropFlow.Domain.Inventory;

/// <summary>
/// Tracks available inventory per RoomType per date.
/// Available = TotalRooms - Sold — the exact format expected by all OTA ARI APIs.
/// Updated by BookingCreationSaga, BookingCancellationSaga, and Room.OutOfOrder events.
/// </summary>
public sealed class Allotment : AggregateRoot
{
    public Guid PropertyId { get; private set; }
    public Guid RoomTypeId { get; private set; }
    public DateOnly Date { get; private set; }
    public int TotalRooms { get; private set; }
    public int Sold { get; private set; }
    /// <summary>Computed. Pushed to channel manager on every change via InventoryUpdated event.</summary>
    public int Available => Math.Max(0, TotalRooms - Sold);

    private Allotment() { }

    public static Allotment Create(
        Guid tenantId, Guid propertyId, Guid roomTypeId, DateOnly date, int totalRooms)
    {
        if (totalRooms < 0) throw DomainError.Validation("TotalRooms must be >= 0.");
        return new Allotment
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            PropertyId = propertyId,
            RoomTypeId = roomTypeId,
            Date = date,
            TotalRooms = totalRooms,
            Sold = 0,
        };
    }

    /// <summary>Called by BookingCreationSaga. Throws if no availability.</summary>
    public void Decrement(int count = 1)
    {
        if (Available < count)
            throw DomainError.Conflict(
                $"No availability for RoomType {RoomTypeId} on {Date}. Available: {Available}, requested: {count}.");
        Sold += count;
        Raise(new InventoryUpdated(PropertyId, RoomTypeId, Date, Available));
    }

    /// <summary>Called by BookingCancellationSaga.</summary>
    public void Increment(int count = 1)
    {
        Sold = Math.Max(0, Sold - count);
        Raise(new InventoryUpdated(PropertyId, RoomTypeId, Date, Available));
    }

    /// <summary>Called when Room.OutOfOrder declared — subtracts from capacity count.</summary>
    public void RemoveRoom()
    {
        TotalRooms = Math.Max(0, TotalRooms - 1);
        Raise(new InventoryUpdated(PropertyId, RoomTypeId, Date, Available));
    }

    /// <summary>Called when Room restored from OutOfOrder.</summary>
    public void RestoreRoom()
    {
        TotalRooms++;
        Raise(new InventoryUpdated(PropertyId, RoomTypeId, Date, Available));
    }
}
