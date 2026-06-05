using MediatR;
using PropFlow.Application.Availability.Queries;
using PropFlow.Application.Channels.Commands;
using PropFlow.Domain.Channels;

namespace PropFlow.Api.Endpoints;

public static class ChannelEndpoints
{
    public static RouteGroupBuilder MapChannelEndpoints(this RouteGroupBuilder group)
    {
        group.MapPost("/",                             ConnectChannel).WithName("ConnectChannel");
        group.MapGet("/{id:guid}/status",              GetChannelStatus).WithName("GetChannelStatus");
        group.MapPost("/{id:guid}/activate",           ActivateChannel).WithName("ActivateChannel");
        group.MapPost("/{id:guid}/suspend",            SuspendChannel).WithName("SuspendChannel");
        group.MapDelete("/{id:guid}",                  DisconnectChannel).WithName("DisconnectChannel");
        group.MapPut("/{id:guid}/mappings/room-types", MapRoomTypes).WithName("MapRoomTypes");
        group.MapPut("/{id:guid}/mappings/rate-plans", MapRatePlans).WithName("MapRatePlans");
        group.MapGet("/availability",                  GetAvailability).WithName("GetAvailability");
        return group;
    }

    private static async Task<IResult> ConnectChannel(
        ConnectChannelRequest req, ISender mediator, HttpContext ctx, CancellationToken ct)
    {
        var id = await mediator.Send(
            new ConnectChannelCommand(
                GetTenantId(ctx), req.PropertyId,
                req.ChannelCode, req.HotelId, req.EncryptedApiKey), ct);
        return Results.Created($"/api/v1/channels/{id}", new { Id = id });
    }

    private static async Task<IResult> GetChannelStatus(
        Guid id, IChannelConnectionRepository repo, CancellationToken ct)
    {
        var conn = await repo.GetAsync(id, ct);
        if (conn is null) return Results.NotFound();
        return Results.Ok(new
        {
            conn.Id,
            conn.ChannelCode,
            Status         = conn.Status.ToString(),
            conn.LastSyncAt,
            LastSyncStatus = conn.LastSyncStatus?.ToString(),
        });
    }

    private static async Task<IResult> ActivateChannel(
        Guid id, ISender mediator, CancellationToken ct)
    {
        await mediator.Send(new ActivateChannelCommand(id), ct);
        return Results.NoContent();
    }

    private static async Task<IResult> SuspendChannel(
        Guid id, ISender mediator, CancellationToken ct)
    {
        await mediator.Send(new SuspendChannelCommand(id), ct);
        return Results.NoContent();
    }

    private static async Task<IResult> DisconnectChannel(
        Guid id, ISender mediator, CancellationToken ct)
    {
        await mediator.Send(new DisconnectChannelCommand(id), ct);
        return Results.NoContent();
    }

    private static async Task<IResult> MapRoomTypes(
        Guid id, MapRoomTypesRequest req, ISender mediator, CancellationToken ct)
    {
        await mediator.Send(new MapRoomTypesCommand(id,
            req.Mappings
               .Select(m => new RoomTypeMappingRequest(m.InternalRoomTypeId, m.ExternalRoomTypeCode))
               .ToList()), ct);
        return Results.NoContent();
    }

    private static async Task<IResult> MapRatePlans(
        Guid id, MapRatePlansRequest req, ISender mediator, CancellationToken ct)
    {
        await mediator.Send(new MapRatePlansCommand(id,
            req.Mappings
               .Select(m => new RatePlanMappingRequest(m.InternalRatePlanId, m.ExternalRatePlanCode))
               .ToList()), ct);
        return Results.NoContent();
    }

    private static async Task<IResult> GetAvailability(
        Guid propertyId, Guid roomTypeId, DateOnly from, DateOnly to,
        IMediator mediator, CancellationToken ct)
    {
        var result = await mediator.Send(
            new GetAvailabilityRangeQuery(propertyId, roomTypeId, from, to), ct);
        return Results.Ok(result);
    }

    private static Guid GetTenantId(HttpContext ctx) =>
        Guid.TryParse(ctx.User.FindFirst("tenant_id")?.Value, out var id) ? id : Guid.Empty;

    private sealed record ConnectChannelRequest(
        Guid PropertyId, string ChannelCode, string HotelId, string EncryptedApiKey);
    private sealed record MapRoomTypesRequest(IReadOnlyList<RoomTypeMappingItem> Mappings);
    private sealed record RoomTypeMappingItem(
        Guid InternalRoomTypeId, string ExternalRoomTypeCode);
    private sealed record MapRatePlansRequest(IReadOnlyList<RatePlanMappingItem> Mappings);
    private sealed record RatePlanMappingItem(
        Guid InternalRatePlanId, string ExternalRatePlanCode);
}
