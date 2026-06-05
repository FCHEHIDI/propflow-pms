namespace PropFlow.Domain.Bookings;

public enum BookingStatus
{
    /// <summary>Direct channel only. Inventory soft-blocked. Awaiting payment guarantee.</summary>
    Tentative,
    /// <summary>Inventory firmly blocked. Confirmation sent to guest.</summary>
    Confirmed,
    /// <summary>Payment guarantee token received. Room guaranteed for late arrival.</summary>
    Guaranteed,
    CheckedIn,
    CheckedOut,
    Cancelled,
    /// <summary>Guest did not arrive. Triggered by night audit. Penalty applies per CancellationPolicy.</summary>
    NoShow,
}

public enum BookingSource
{
    Direct,
    WebDirect,
    OTA,
    GDS,
    Wholesale,
}
