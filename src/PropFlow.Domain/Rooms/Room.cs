using PropFlow.Domain.Common;
using PropFlow.Domain.Errors;
using PropFlow.Domain.Events;

namespace PropFlow.Domain.Rooms;

public sealed class Room : AggregateRoot
{
    public Guid PropertyId { get; private set; }
    public Guid RoomTypeId { get; private set; }
    /// <summary>Invariant: unique per PropertyId.</summary>
    public string RoomNumber { get; private set; } = default!;
    public int Floor { get; private set; }
    public string? Wing { get; private set; }
    public string? Building { get; private set; }
    public decimal SquareMeters { get; private set; }
    public Guid BedTypeId { get; private set; }
    public Guid ViewTypeId { get; private set; }
    public RoomStatus Status { get; private set; }
    /// <summary>Invariant: non-null if and only if Status == Occupied.</summary>
    public OccupancyKind? Occupancy { get; private set; }
    public string? Notes { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private Room() { }

    public static Room Create(
        Guid tenantId,
        Guid propertyId,
        Guid roomTypeId,
        string roomNumber,
        int floor,
        decimal squareMeters,
        Guid bedTypeId,
        Guid viewTypeId,
        string? wing = null,
        string? building = null)
    {
        if (string.IsNullOrWhiteSpace(roomNumber))
            throw DomainError.Validation("Room number is required.");
        if (squareMeters <= 0)
            throw DomainError.Validation("SquareMeters must be positive.");

        var room = new Room
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            PropertyId = propertyId,
            RoomTypeId = roomTypeId,
            RoomNumber = roomNumber,
            Floor = floor,
            Wing = wing,
            Building = building,
            SquareMeters = squareMeters,
            BedTypeId = bedTypeId,
            ViewTypeId = viewTypeId,
            Status = RoomStatus.Available,
            CreatedAt = DateTime.UtcNow,
        };

        // Raise domain event so RoomStatusBoardProjection can create the initial read model.
        room.Raise(new RoomCreated(room.Id, propertyId, roomTypeId, roomNumber, floor, wing, building));
        return room;
    }

    /// <summary>Triggered by CheckInCommand. Invariant: Occupancy becomes non-null.</summary>
    public void Occupy(OccupancyKind kind)
    {
        if (Status is not (RoomStatus.Available or RoomStatus.Inspected))
            throw DomainError.InvalidState($"Cannot occupy room in state {Status}.");

        var previous = Status;
        Status = RoomStatus.Occupied;
        Occupancy = kind;
        Raise(new RoomStatusChanged(Id, PropertyId, previous, RoomStatus.Occupied));
    }

    /// <summary>Triggered by CheckOutCommand. Invariant: Occupancy becomes null.</summary>
    public void Vacate()
    {
        if (Status != RoomStatus.Occupied)
            throw DomainError.InvalidState($"Cannot vacate room in state {Status}.");

        Status = RoomStatus.VacantDirty;
        Occupancy = null;
        Raise(new RoomStatusChanged(Id, PropertyId, RoomStatus.Occupied, RoomStatus.VacantDirty));
    }

    public void BeginCleaning()
    {
        if (Status is not (RoomStatus.VacantDirty or RoomStatus.Available))
            throw DomainError.InvalidState($"Cannot begin cleaning room in state {Status}.");

        var previous = Status;
        Status = RoomStatus.OnChange;
        Raise(new RoomStatusChanged(Id, PropertyId, previous, RoomStatus.OnChange));
    }

    /// <summary>
    /// Housekeeper completes visual inspection.
    /// Invariant: mandatory before Available for DayUse turnaround safety.
    /// </summary>
    public void CompleteInspection()
    {
        if (Status != RoomStatus.OnChange)
            throw DomainError.InvalidState($"Cannot complete inspection for room in state {Status}.");

        Status = RoomStatus.Inspected;
        Raise(new RoomStatusChanged(Id, PropertyId, RoomStatus.OnChange, RoomStatus.Inspected));
    }

    public void MarkAvailable()
    {
        if (Status is not (RoomStatus.Inspected or RoomStatus.OutOfOrder or RoomStatus.OutOfService))
            throw DomainError.InvalidState($"Cannot mark room available from state {Status}.");

        var previous = Status;
        Status = RoomStatus.Available;
        Occupancy = null;
        Raise(new RoomStatusChanged(Id, PropertyId, previous, RoomStatus.Available));

        if (previous == RoomStatus.OutOfOrder)
            Raise(new RoomRestoredToInventory(Id, PropertyId, RoomTypeId));
    }

    /// <summary>
    /// Withdraws room from inventory AND capacity count.
    /// Invariant: triggers Allotment.RemoveRoom() for all future dates via domain event.
    /// </summary>
    public void DeclareOutOfOrder(string? reason = null)
    {
        if (Status == RoomStatus.Occupied)
            throw DomainError.InvalidState("Cannot declare an occupied room Out of Order.");

        var previous = Status;
        Status = RoomStatus.OutOfOrder;
        Occupancy = null;
        Notes = reason;
        Raise(new RoomStatusChanged(Id, PropertyId, previous, RoomStatus.OutOfOrder));
        Raise(new RoomRemovedFromInventory(Id, PropertyId, RoomTypeId));
    }

    /// <summary>Soft block — unavailable but capacity count unaffected.</summary>
    public void DeclareOutOfService(string? reason = null)
    {
        if (Status == RoomStatus.Occupied)
            throw DomainError.InvalidState("Cannot declare an occupied room Out of Service.");

        var previous = Status;
        Status = RoomStatus.OutOfService;
        Occupancy = null;
        Notes = reason;
        Raise(new RoomStatusChanged(Id, PropertyId, previous, RoomStatus.OutOfService));
    }

    public void UpdateNotes(string? notes) => Notes = notes;
}
