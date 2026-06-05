using PropFlow.Domain.Common;
using PropFlow.Domain.Errors;

namespace PropFlow.Domain.Referentials;

public sealed class BedType : AggregateRoot
{
    public string Code { get; private set; } = default!;
    public string Label { get; private set; } = default!;
    /// <summary>Max occupants for this bed configuration. Used to derive Room.MaxOccupancy.</summary>
    public int Capacity { get; private set; }

    private BedType() { }

    public static BedType Create(Guid tenantId, string code, string label, int capacity)
    {
        if (capacity < 1) throw DomainError.Validation("BedType capacity must be >= 1.");
        return new BedType
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Code = code,
            Label = label,
            Capacity = capacity,
        };
    }
}
