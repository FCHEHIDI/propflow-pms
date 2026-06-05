using PropFlow.Domain.Common;
using PropFlow.Domain.Errors;

namespace PropFlow.Domain.Properties;

public sealed class Property : AggregateRoot
{
    public string Name { get; private set; } = default!;
    public Address Address { get; private set; } = default!;
    public int? StarRating { get; private set; }
    /// <summary>IANA timezone identifier — critical for check-in/out/night-audit rule evaluation.</summary>
    public string TimeZone { get; private set; } = default!;
    public TimeOnly DefaultCheckInTime { get; private set; }
    public TimeOnly DefaultCheckOutTime { get; private set; }
    /// <summary>ISO 4217. Invariant: immutable after first RatePlan is published.</summary>
    public string Currency { get; private set; } = default!;
    public bool IsCurrencyLocked { get; private set; }
    public IReadOnlyList<Guid> CommonAmenityIds => _commonAmenityIds.AsReadOnly();
    private readonly List<Guid> _commonAmenityIds = [];
    public DateTime CreatedAt { get; private set; }

    private Property() { }

    public static Property Create(
        Guid tenantId,
        string name,
        Address address,
        string timeZone,
        TimeOnly checkInTime,
        TimeOnly checkOutTime,
        string currency,
        int? starRating = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw DomainError.Validation("Property name is required.");
        if (starRating is < 1 or > 5)
            throw DomainError.Validation("Star rating must be between 1 and 5.");
        if (string.IsNullOrWhiteSpace(currency) || currency.Length != 3)
            throw DomainError.Validation("Currency must be a valid ISO 4217 code (3 letters).");

        return new Property
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Name = name,
            Address = address,
            StarRating = starRating,
            TimeZone = timeZone,
            DefaultCheckInTime = checkInTime,
            DefaultCheckOutTime = checkOutTime,
            Currency = currency,
            CreatedAt = DateTime.UtcNow,
        };
    }

    /// <summary>Called when the first RatePlan is published. Currency becomes immutable.</summary>
    public void LockCurrency()
    {
        if (IsCurrencyLocked) return;
        IsCurrencyLocked = true;
    }

    public void UpdateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw DomainError.Validation("Property name is required.");
        Name = name;
    }

    public void AddCommonAmenity(Guid amenityId)
    {
        if (!_commonAmenityIds.Contains(amenityId))
            _commonAmenityIds.Add(amenityId);
    }

    public void RemoveCommonAmenity(Guid amenityId) =>
        _commonAmenityIds.Remove(amenityId);
}
