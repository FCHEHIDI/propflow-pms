using PropFlow.Domain.Errors;

namespace PropFlow.Domain.RoomTypes;

public sealed record SquareMetersRange
{
    public decimal Min { get; }
    public decimal Max { get; }

    public SquareMetersRange(decimal min, decimal max)
    {
        if (max < min) throw DomainError.Validation("SquareMeters max must be >= min.");
        Min = min;
        Max = max;
    }

    /// <summary>Invariant checked at Room ↔ RoomType assignment.</summary>
    public bool Contains(decimal value) => value >= Min && value <= Max;

    public override string ToString() => $"[{Min:F1}, {Max:F1}] m²";
}
