using PropFlow.Domain.Common;
using PropFlow.Domain.Errors;
using PropFlow.Domain.Events;

namespace PropFlow.Domain.RatePlans;

public sealed class RatePlan : AggregateRoot
{
    public Guid PropertyId { get; private set; }
    public RatePlanStatus Status { get; private set; }
    /// <summary>Invariant: unique per PropertyId. Used by channel manager for OTA mapping.</summary>
    public string Code { get; private set; } = default!;
    public string Name { get; private set; } = default!;
    public MealPlan MealPlan { get; private set; }
    public Guid CancellationPolicyId { get; private set; }
    /// <summary>false = negotiated/corporate rate — not pushed to OTAs.</summary>
    public bool IsPublic { get; private set; }
    public IReadOnlyList<RoomTypePrice> Prices => _prices.AsReadOnly();
    private readonly List<RoomTypePrice> _prices = [];
    public DateTime CreatedAt { get; private set; }

    private RatePlan() { }

    public static RatePlan Create(
        Guid tenantId,
        Guid propertyId,
        string code,
        string name,
        MealPlan mealPlan,
        Guid cancellationPolicyId,
        bool isPublic = true)
    {
        if (string.IsNullOrWhiteSpace(code)) throw DomainError.Validation("RatePlan code is required.");
        if (string.IsNullOrWhiteSpace(name)) throw DomainError.Validation("RatePlan name is required.");

        return new RatePlan
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            PropertyId = propertyId,
            Code = code.ToUpperInvariant(),
            Name = name,
            MealPlan = mealPlan,
            CancellationPolicyId = cancellationPolicyId,
            IsPublic = isPublic,
            Status = RatePlanStatus.Draft,
            CreatedAt = DateTime.UtcNow,
        };
    }

    /// <summary>Upserts the price for a given RoomType. Currency = Property.Currency (implicit).</summary>
    public void SetPrice(Guid roomTypeId, decimal baseRate, decimal? extraAdult = null, decimal? extraChild = null)
    {
        if (baseRate < 0) throw DomainError.Validation("BaseRate must be >= 0.");
        _prices.RemoveAll(p => p.RoomTypeId == roomTypeId);
        _prices.Add(new RoomTypePrice(roomTypeId, baseRate, extraAdult, extraChild));
    }

    /// <summary>Invariant: must have at least one price before publishing.</summary>
    public void Publish()
    {
        if (Status is not (RatePlanStatus.Draft or RatePlanStatus.Suspended))
            throw DomainError.InvalidState($"Cannot publish RatePlan in state {Status}.");
        if (_prices.Count == 0)
            throw DomainError.Validation("RatePlan must have at least one price before publishing.");

        Status = RatePlanStatus.Active;
        Raise(new RatePlanPublished(Id, PropertyId, IsPublic));
    }

    public void Suspend()
    {
        if (Status != RatePlanStatus.Active)
            throw DomainError.InvalidState($"Cannot suspend RatePlan in state {Status}.");
        Status = RatePlanStatus.Suspended;
    }

    public void Archive()
    {
        if (Status == RatePlanStatus.Archived) return;
        Status = RatePlanStatus.Archived;
        Raise(new RatePlanArchived(Id, PropertyId));
    }

    public RoomTypePrice? GetPriceFor(Guid roomTypeId) =>
        _prices.Find(p => p.RoomTypeId == roomTypeId);

    public RatePlanSnapshot TakeSnapshot(Guid roomTypeId)
    {
        var price = GetPriceFor(roomTypeId)
            ?? throw DomainError.NotFound($"No price configured for RoomType {roomTypeId} in RatePlan {Code}.");

        return new RatePlanSnapshot(
            Id, Code, Name, MealPlan.ToString(),
            price.BaseRate, price.ExtraAdultRate, price.ExtraChildRate,
            CancellationPolicyId);
    }
}

public sealed record RoomTypePrice(
    Guid RoomTypeId,
    decimal BaseRate,
    decimal? ExtraAdultRate,
    decimal? ExtraChildRate);
