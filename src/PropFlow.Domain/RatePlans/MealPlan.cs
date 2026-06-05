namespace PropFlow.Domain.RatePlans;

/// <summary>Standard OTA meal plan codes — used verbatim in ARI messages.</summary>
public enum MealPlan
{
    RoomOnly,
    BedAndBreakfast,
    HalfBoard,
    FullBoard,
    AllInclusive,
}

public enum RatePlanStatus
{
    Draft,
    Active,
    Suspended,
    Archived,
}
