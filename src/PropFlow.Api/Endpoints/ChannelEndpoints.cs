using MediatR;
using PropFlow.Application.Availability.Queries;

namespace PropFlow.Api.Endpoints;

public static class ChannelEndpoints
{
    public static RouteGroupBuilder MapChannelEndpoints(this RouteGroupBuilder group)
    {
        group.MapPost("/",                              ConnectChannel).WithName("ConnectChannel");
        group.MapGet("/{id:guid}/status",               GetChannelStatus).WithName("GetChannelStatus");
        group.MapPost("/{id:guid}/activate",            ActivateChannel).WithName("ActivateChannel");
        group.MapPost("/{id:guid}/suspend",             SuspendChannel).WithName("SuspendChannel");
        group.MapDelete("/{id:guid}",                   DisconnectChannel).WithName("DisconnectChannel");
        group.MapPut("/{id:guid}/mappings/room-types",  MapRoomTypes).WithName("MapRoomTypes");
        group.MapPut("/{id:guid}/mappings/rate-plans",  MapRatePlans).WithName("MapRatePlans");
        group.MapGet("/availability",                   GetAvailability).WithName("GetAvailability");
        return group;
    }

    private static IResult ConnectChannel()           => Results.Ok();        // TODO
    private static IResult GetChannelStatus(Guid id)  => Results.Ok();        // TODO
    private static IResult ActivateChannel(Guid id)   => Results.NoContent(); // TODO
    private static IResult SuspendChannel(Guid id)    => Results.NoContent(); // TODO
    private static IResult DisconnectChannel(Guid id) => Results.NoContent(); // TODO
    private static IResult MapRoomTypes(Guid id)      => Results.NoContent(); // TODO
    private static IResult MapRatePlans(Guid id)      => Results.NoContent(); // TODO

    /// <summary>
    /// GET /api/v1/channels/availability?propertyId=...&amp;roomTypeId=...&amp;from=2026-06-01&amp;to=2026-06-30
    /// Returns AvailabilityView per day. Used by channel manager before pushing ARI to OTAs.
    /// </summary>
    private static async Task<IResult> GetAvailability(
        Guid propertyId,
        Guid roomTypeId,
        DateOnly from,
        DateOnly to,
        IMediator mediator,
        CancellationToken ct)
    {
        var result = await mediator.Send(
            new GetAvailabilityRangeQuery(propertyId, roomTypeId, from, to), ct);
        return Results.Ok(result);
    }
}
