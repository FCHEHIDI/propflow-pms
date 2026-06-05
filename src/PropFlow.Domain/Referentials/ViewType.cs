using PropFlow.Domain.Common;

namespace PropFlow.Domain.Referentials;

public sealed class ViewType : AggregateRoot
{
    public string Code { get; private set; } = default!;
    public string Label { get; private set; } = default!;

    private ViewType() { }

    public static ViewType Create(Guid tenantId, string code, string label) =>
        new() { Id = Guid.NewGuid(), TenantId = tenantId, Code = code, Label = label };
}
