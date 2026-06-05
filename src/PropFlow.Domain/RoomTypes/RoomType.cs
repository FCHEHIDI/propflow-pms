using PropFlow.Domain.Common;
using PropFlow.Domain.Errors;
using PropFlow.Domain.Events;

namespace PropFlow.Domain.RoomTypes;

public sealed class RoomType : AggregateRoot
{
    public Guid PropertyId { get; private set; }
    public RoomTypeStatus Status { get; private set; }
    public int CurrentVersion { get; private set; }
    public string Label { get; private set; } = default!;
    public string? Description { get; private set; }
    public SquareMetersRange SquareMetersRange { get; private set; } = default!;
    public IReadOnlyList<Guid> AllowedBedTypeIds => _allowedBedTypeIds.AsReadOnly();
    private readonly List<Guid> _allowedBedTypeIds = [];
    public IReadOnlyList<Guid> AllowedViewTypeIds => _allowedViewTypeIds.AsReadOnly();
    private readonly List<Guid> _allowedViewTypeIds = [];
    /// <summary>Invariant: MaxOccupancy >= BaseOccupancy.</summary>
    public int BaseOccupancy { get; private set; }
    public int MaxOccupancy { get; private set; }
    /// <summary>In addition to Property.CommonAmenityIds.</summary>
    public IReadOnlyList<Guid> SpecificAmenityIds => _specificAmenityIds.AsReadOnly();
    private readonly List<Guid> _specificAmenityIds = [];
    public DateTime CreatedAt { get; private set; }

    private RoomType() { }

    public static RoomType Create(
        Guid tenantId,
        Guid propertyId,
        string label,
        SquareMetersRange squareMetersRange,
        int baseOccupancy,
        int maxOccupancy,
        string? description = null)
    {
        if (string.IsNullOrWhiteSpace(label))
            throw DomainError.Validation("RoomType label is required.");
        if (maxOccupancy < baseOccupancy)
            throw DomainError.Validation("MaxOccupancy must be >= BaseOccupancy.");

        return new RoomType
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            PropertyId = propertyId,
            Status = RoomTypeStatus.Draft,
            CurrentVersion = 1,
            Label = label,
            Description = description,
            SquareMetersRange = squareMetersRange,
            BaseOccupancy = baseOccupancy,
            MaxOccupancy = maxOccupancy,
            CreatedAt = DateTime.UtcNow,
        };
    }

    public void Publish()
    {
        if (Status is not (RoomTypeStatus.Draft or RoomTypeStatus.Suspended))
            throw DomainError.InvalidState($"Cannot publish RoomType in state {Status}.");

        Status = RoomTypeStatus.Active;
        Raise(new RoomTypePublished(Id, PropertyId));
    }

    public void Suspend()
    {
        if (Status != RoomTypeStatus.Active)
            throw DomainError.InvalidState($"Cannot suspend RoomType in state {Status}.");

        Status = RoomTypeStatus.Suspended;
        Raise(new RoomTypeSuspended(Id, PropertyId));
    }

    /// <summary>Invariant: Deprecated is terminal. Non-deletable — snapshots in Bookings reference this.</summary>
    public void Deprecate()
    {
        if (Status == RoomTypeStatus.Deprecated) return;
        Status = RoomTypeStatus.Deprecated;
        Raise(new RoomTypeDeprecated(Id, PropertyId));
    }

    /// <summary>
    /// Increments CurrentVersion. Any subsequent Booking will take a new snapshot.
    /// Existing Booking snapshots are unaffected.
    /// </summary>
    public void UpdateContent(
        string label,
        string? description,
        SquareMetersRange squareMetersRange,
        int baseOccupancy,
        int maxOccupancy)
    {
        if (Status == RoomTypeStatus.Deprecated)
            throw DomainError.InvalidState("Cannot update a deprecated RoomType.");
        if (maxOccupancy < baseOccupancy)
            throw DomainError.Validation("MaxOccupancy must be >= BaseOccupancy.");

        Label = label;
        Description = description;
        SquareMetersRange = squareMetersRange;
        BaseOccupancy = baseOccupancy;
        MaxOccupancy = maxOccupancy;
        CurrentVersion++;
    }

    public void AddBedType(Guid bedTypeId)
    {
        if (!_allowedBedTypeIds.Contains(bedTypeId))
            _allowedBedTypeIds.Add(bedTypeId);
    }

    public void AddViewType(Guid viewTypeId)
    {
        if (!_allowedViewTypeIds.Contains(viewTypeId))
            _allowedViewTypeIds.Add(viewTypeId);
    }

    public void AddAmenity(Guid amenityId)
    {
        if (!_specificAmenityIds.Contains(amenityId))
            _specificAmenityIds.Add(amenityId);
    }

    /// <summary>Produces the immutable snapshot stored in Booking at creation time.</summary>
    public RoomTypeSnapshot TakeSnapshot() => new(
        Id,
        CurrentVersion,
        Label,
        Description,
        BaseOccupancy,
        MaxOccupancy,
        _specificAmenityIds.ToList());
}
