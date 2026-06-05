namespace PropFlow.Domain.Rooms;

public enum RoomStatus
{
    Available,
    Occupied,
    VacantDirty,
    OnChange,
    Inspected,
    /// <summary>Withdrawn from inventory + capacity count. Affects allotment TotalRooms.</summary>
    OutOfOrder,
    /// <summary>Temporarily unavailable. Does NOT affect capacity count.</summary>
    OutOfService,
}

public enum OccupancyKind
{
    /// <summary>Standard overnight stay.</summary>
    Overnight,
    /// <summary>Same-day stay. Triggers urgent housekeeping turnaround.</summary>
    DayUse,
    /// <summary>Internal use (staff, demo). Revenue excluded from metrics.</summary>
    HouseUse,
}
