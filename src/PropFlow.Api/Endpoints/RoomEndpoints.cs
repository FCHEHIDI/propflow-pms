using MediatR;
using PropFlow.Application.Rooms.Commands;
using PropFlow.Application.Rooms.Queries;

namespace PropFlow.Api.Endpoints;

public static class RoomEndpoints
{
    public static RouteGroupBuilder MapRoomEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/",                                GetRooms).WithName("GetRooms");
        group.MapPost("/",                               CreateRoom).WithName("CreateRoom");
        group.MapPost("/{id:guid}/housekeeping/clean",   BeginCleaning).WithName("BeginCleaning");
        group.MapPost("/{id:guid}/housekeeping/inspect", CompleteInspection).WithName("CompleteInspection");
        group.MapPost("/{id:guid}/housekeeping/clear",   MarkAvailable).WithName("MarkAvailable");
        group.MapPost("/{id:guid}/out-of-order",         DeclareOutOfOrder).WithName("DeclareOutOfOrder");
        group.MapPost("/{id:guid}/out-of-service",       DeclareOutOfService).WithName("DeclareOutOfService");
        return group;
    }

    /// <summary>
    /// GET /api/v1/rooms?propertyId=...
    /// Returns RoomStatusBoardView ordered by Floor + RoomNumber.
    /// Consumed by housekeeping tablets and IoTPanelService.
    /// </summary>
    private static async Task<IResult> GetRooms(
        Guid propertyId,
        IMediator mediator,
        CancellationToken ct)
    {
        var result = await mediator.Send(new GetRoomStatusBoardQuery(propertyId), ct);
        return Results.Ok(result);
    }

    private static async Task<IResult> CreateRoom(
        CreateRoomRequest req, IMediator mediator, CancellationToken ct)
    {
        var cmd = new CreateRoomCommand(
            req.TenantId, req.PropertyId, req.RoomTypeId,
            req.RoomNumber, req.Floor, req.SquareMeters,
            req.BedTypeId, req.ViewTypeId, req.Wing, req.Building);
        var roomId = await mediator.Send(cmd, ct);
        return Results.Created($"/api/v1/rooms/{roomId}", new { roomId });
    }

    private static async Task<IResult> BeginCleaning(
        Guid id, IMediator mediator, CancellationToken ct)
    {
        await mediator.Send(new BeginCleaningCommand(id), ct);
        return Results.NoContent();
    }

    private static async Task<IResult> CompleteInspection(
        Guid id, IMediator mediator, CancellationToken ct)
    {
        await mediator.Send(new CompleteInspectionCommand(id), ct);
        return Results.NoContent();
    }

    private static async Task<IResult> MarkAvailable(
        Guid id, IMediator mediator, CancellationToken ct)
    {
        await mediator.Send(new MarkRoomAvailableCommand(id), ct);
        return Results.NoContent();
    }

    private static async Task<IResult> DeclareOutOfOrder(
        Guid id, BlockRequest? req, IMediator mediator, CancellationToken ct)
    {
        await mediator.Send(new DeclareOutOfOrderCommand(id, req?.Reason), ct);
        return Results.NoContent();
    }

    private static IResult DeclareOutOfService(Guid id) => Results.NoContent(); // TODO
}

public sealed record CreateRoomRequest(
    Guid TenantId,
    Guid PropertyId,
    Guid RoomTypeId,
    string RoomNumber,
    int Floor,
    decimal SquareMeters,
    Guid BedTypeId,
    Guid ViewTypeId,
    string? Wing = null,
    string? Building = null);

public sealed record BlockRequest(string? Reason);
