namespace PropFlow.Domain.RoomTypes;

public enum RoomTypeStatus
{
    Draft,
    Active,
    Suspended,
    /// <summary>
    /// Terminal for new bookings. Non-deletable — referenced by historical booking snapshots.
    /// </summary>
    Deprecated,
}
