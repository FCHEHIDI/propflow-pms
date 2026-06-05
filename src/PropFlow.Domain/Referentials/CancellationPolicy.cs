using PropFlow.Domain.Common;
using PropFlow.Domain.Errors;

namespace PropFlow.Domain.Referentials;

public sealed class CancellationPolicy : AggregateRoot
{
    public string Name { get; private set; } = default!;
    public PolicyTemplate? BaseTemplate { get; private set; }
    public int? DeadlineDays { get; private set; }
    /// <summary>Invariant: mutually exclusive with PenaltyPercent.</summary>
    public int? PenaltyNights { get; private set; }
    /// <summary>Invariant: mutually exclusive with PenaltyNights.</summary>
    public decimal? PenaltyPercent { get; private set; }
    public bool IsDefault { get; private set; }

    private CancellationPolicy() { }

    public static CancellationPolicy Create(
        Guid tenantId,
        string name,
        int? deadlineDays,
        int? penaltyNights = null,
        decimal? penaltyPercent = null,
        PolicyTemplate? baseTemplate = null,
        bool isDefault = false)
    {
        if (penaltyNights.HasValue && penaltyPercent.HasValue)
            throw DomainError.Validation("PenaltyNights and PenaltyPercent are mutually exclusive.");

        return new CancellationPolicy
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Name = name,
            BaseTemplate = baseTemplate,
            DeadlineDays = deadlineDays,
            PenaltyNights = penaltyNights,
            PenaltyPercent = penaltyPercent,
            IsDefault = isDefault,
        };
    }
}

public enum PolicyTemplate
{
    NonRefundable,   // Deadline: 0, Penalty: 100%
    Flexible,        // Deadline: 1, Penalty: 0%
    Moderate,        // Deadline: 5, Penalty: 0% then 1 night
    Strict,          // Deadline: 14, Penalty: 50% <14d, 100% <7d
}
