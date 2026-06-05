namespace PropFlow.Domain.Guests;

public enum GuestStatus
{
    /// <summary>OTA booking without profile — name only.</summary>
    Anonymous,
    /// <summary>Profile created with email and/or preferences.</summary>
    Profiled,
    /// <summary>Identity verified at physical check-in.</summary>
    Verified,
    /// <summary>Flagged by property (unpaid balance, property damage).</summary>
    Blacklisted,
}
