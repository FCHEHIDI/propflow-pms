using MassTransit;
using MediatR;
using PropFlow.Domain.Channels;
using PropFlow.Domain.Errors;

namespace PropFlow.Application.Channels.Commands;

// ─── Connect ────────────────────────────────────────────────────────────────────
public sealed record ConnectChannelCommand(
    Guid TenantId,
    Guid PropertyId,
    string ChannelCode,
    string HotelId,
    string EncryptedApiKey) : IRequest<Guid>;

public sealed class ConnectChannelHandler : IRequestHandler<ConnectChannelCommand, Guid>
{
    private readonly IChannelConnectionRepository _repo;
    private readonly IPublishEndpoint _bus;

    public ConnectChannelHandler(IChannelConnectionRepository repo, IPublishEndpoint bus)
    {
        _repo = repo;
        _bus  = bus;
    }

    public async Task<Guid> Handle(ConnectChannelCommand cmd, CancellationToken ct)
    {
        var creds = new EncryptedCredentials(cmd.HotelId, cmd.EncryptedApiKey);
        var connection = ChannelConnection.Create(
            cmd.TenantId, cmd.PropertyId, cmd.ChannelCode, creds);
        await _repo.SaveAsync(connection, ct);
        foreach (var e in connection.DomainEvents) await _bus.Publish(e, ct);
        connection.ClearDomainEvents();
        return connection.Id;
    }
}

// ─── Map Room Types ───────────────────────────────────────────────────────────────
public sealed record MapRoomTypesCommand(
    Guid ConnectionId,
    IReadOnlyList<RoomTypeMappingRequest> Mappings) : IRequest;

public sealed record RoomTypeMappingRequest(Guid InternalRoomTypeId, string ExternalRoomTypeCode);

public sealed class MapRoomTypesHandler : IRequestHandler<MapRoomTypesCommand>
{
    private readonly IChannelConnectionRepository _repo;
    public MapRoomTypesHandler(IChannelConnectionRepository repo) => _repo = repo;

    public async Task Handle(MapRoomTypesCommand cmd, CancellationToken ct)
    {
        var conn = await _repo.GetAsync(cmd.ConnectionId, ct)
            ?? throw DomainError.NotFound($"ChannelConnection {cmd.ConnectionId} not found.");
        foreach (var m in cmd.Mappings)
            conn.MapRoomType(m.InternalRoomTypeId, m.ExternalRoomTypeCode);
        await _repo.SaveAsync(conn, ct);
    }
}

// ─── Map Rate Plans ───────────────────────────────────────────────────────────────
public sealed record MapRatePlansCommand(
    Guid ConnectionId,
    IReadOnlyList<RatePlanMappingRequest> Mappings) : IRequest;

public sealed record RatePlanMappingRequest(Guid InternalRatePlanId, string ExternalRatePlanCode);

public sealed class MapRatePlansHandler : IRequestHandler<MapRatePlansCommand>
{
    private readonly IChannelConnectionRepository _repo;
    public MapRatePlansHandler(IChannelConnectionRepository repo) => _repo = repo;

    public async Task Handle(MapRatePlansCommand cmd, CancellationToken ct)
    {
        var conn = await _repo.GetAsync(cmd.ConnectionId, ct)
            ?? throw DomainError.NotFound($"ChannelConnection {cmd.ConnectionId} not found.");
        foreach (var m in cmd.Mappings)
            conn.MapRatePlan(m.InternalRatePlanId, m.ExternalRatePlanCode);
        await _repo.SaveAsync(conn, ct);
    }
}

// ─── Activate ────────────────────────────────────────────────────────────────────
public sealed record ActivateChannelCommand(Guid ConnectionId) : IRequest;

public sealed class ActivateChannelHandler : IRequestHandler<ActivateChannelCommand>
{
    private readonly IChannelConnectionRepository _repo;
    private readonly IPublishEndpoint _bus;

    public ActivateChannelHandler(IChannelConnectionRepository repo, IPublishEndpoint bus)
    {
        _repo = repo;
        _bus  = bus;
    }

    public async Task Handle(ActivateChannelCommand cmd, CancellationToken ct)
    {
        var conn = await _repo.GetAsync(cmd.ConnectionId, ct)
            ?? throw DomainError.NotFound($"ChannelConnection {cmd.ConnectionId} not found.");
        conn.Activate();
        await _repo.SaveAsync(conn, ct);
        foreach (var e in conn.DomainEvents) await _bus.Publish(e, ct);
        conn.ClearDomainEvents();
    }
}

// ─── Suspend ────────────────────────────────────────────────────────────────────
public sealed record SuspendChannelCommand(Guid ConnectionId) : IRequest;

public sealed class SuspendChannelHandler : IRequestHandler<SuspendChannelCommand>
{
    private readonly IChannelConnectionRepository _repo;
    public SuspendChannelHandler(IChannelConnectionRepository repo) => _repo = repo;

    public async Task Handle(SuspendChannelCommand cmd, CancellationToken ct)
    {
        var conn = await _repo.GetAsync(cmd.ConnectionId, ct)
            ?? throw DomainError.NotFound($"ChannelConnection {cmd.ConnectionId} not found.");
        conn.Suspend();
        await _repo.SaveAsync(conn, ct);
    }
}

// ─── Disconnect ──────────────────────────────────────────────────────────────────
public sealed record DisconnectChannelCommand(Guid ConnectionId) : IRequest;

public sealed class DisconnectChannelHandler : IRequestHandler<DisconnectChannelCommand>
{
    private readonly IChannelConnectionRepository _repo;
    private readonly IPublishEndpoint _bus;

    public DisconnectChannelHandler(IChannelConnectionRepository repo, IPublishEndpoint bus)
    {
        _repo = repo;
        _bus  = bus;
    }

    public async Task Handle(DisconnectChannelCommand cmd, CancellationToken ct)
    {
        var conn = await _repo.GetAsync(cmd.ConnectionId, ct)
            ?? throw DomainError.NotFound($"ChannelConnection {cmd.ConnectionId} not found.");
        conn.Disconnect();
        await _repo.SaveAsync(conn, ct);
        foreach (var e in conn.DomainEvents) await _bus.Publish(e, ct);
        conn.ClearDomainEvents();
    }
}
