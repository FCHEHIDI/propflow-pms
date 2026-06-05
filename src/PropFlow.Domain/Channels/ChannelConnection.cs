using PropFlow.Domain.Common;
using PropFlow.Domain.Errors;
using PropFlow.Domain.Events;

namespace PropFlow.Domain.Channels;

public sealed class ChannelConnection : AggregateRoot
{
    public Guid PropertyId { get; private set; }
    /// <summary>"booking.com" | "expedia" | "airbnb" — lowercase, stable identifier.</summary>
    public string ChannelCode { get; private set; } = default!;
    public ChannelConnectionStatus Status { get; private set; }
    /// <summary>Encrypted at rest. Never logged or serialised in plain text.</summary>
    public EncryptedCredentials Credentials { get; private set; } = default!;
    public IReadOnlyList<RoomTypeMapping> RoomTypeMappings => _roomTypeMappings.AsReadOnly();
    private readonly List<RoomTypeMapping> _roomTypeMappings = [];
    public IReadOnlyList<RatePlanMapping> RatePlanMappings => _ratePlanMappings.AsReadOnly();
    private readonly List<RatePlanMapping> _ratePlanMappings = [];
    public DateTime? LastSyncAt { get; private set; }
    public ChannelSyncStatus? LastSyncStatus { get; private set; }

    private ChannelConnection() { }

    public static ChannelConnection Create(
        Guid tenantId,
        Guid propertyId,
        string channelCode,
        EncryptedCredentials credentials)
    {
        if (string.IsNullOrWhiteSpace(channelCode))
            throw DomainError.Validation("Channel code is required.");

        return new ChannelConnection
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            PropertyId = propertyId,
            ChannelCode = channelCode.ToLowerInvariant(),
            Credentials = credentials,
            Status = ChannelConnectionStatus.Disconnected,
        };
    }

    /// <summary>Invariant: all Active+Public RoomTypes and RatePlans must be mapped before activation.</summary>
    public void Activate()
    {
        if (Status == ChannelConnectionStatus.Active) return;
        Status = ChannelConnectionStatus.Active;
        Raise(new ChannelConnected(Id, PropertyId, ChannelCode));
    }

    public void Suspend() => Status = ChannelConnectionStatus.Suspended;

    public void Disconnect()
    {
        Status = ChannelConnectionStatus.Disconnected;
        Raise(new ChannelDisconnected(Id, PropertyId, ChannelCode));
    }

    public void MapRoomType(Guid internalId, string externalCode)
    {
        _roomTypeMappings.RemoveAll(m => m.InternalRoomTypeId == internalId);
        _roomTypeMappings.Add(new RoomTypeMapping(internalId, externalCode));
    }

    public void MapRatePlan(Guid internalId, string externalCode)
    {
        _ratePlanMappings.RemoveAll(m => m.InternalRatePlanId == internalId);
        _ratePlanMappings.Add(new RatePlanMapping(internalId, externalCode));
    }

    public void RecordSync(ChannelSyncStatus syncStatus)
    {
        LastSyncAt = DateTime.UtcNow;
        LastSyncStatus = syncStatus;
    }
}

public sealed record EncryptedCredentials(string HotelId, string EncryptedApiKey);
public sealed record RoomTypeMapping(Guid InternalRoomTypeId, string ExternalRoomTypeCode);
public sealed record RatePlanMapping(Guid InternalRatePlanId, string ExternalRatePlanCode);

public enum ChannelConnectionStatus { Active, Suspended, Disconnected }
public enum ChannelSyncStatus { Success, Failed, Partial }
