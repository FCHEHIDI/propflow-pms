using PropFlow.Domain.Common;

namespace PropFlow.Domain.Referentials;

public sealed class Amenity : AggregateRoot
{
    public string Code { get; private set; } = default!;
    public string Label { get; private set; } = default!;
    /// <summary>"room" = shown on room card | "property" = shown on hotel card.</summary>
    public string Category { get; private set; } = default!;

    private Amenity() { }

    public static Amenity Create(Guid tenantId, string code, string label, string category) =>
        new() { Id = Guid.NewGuid(), TenantId = tenantId, Code = code, Label = label, Category = category };
}
