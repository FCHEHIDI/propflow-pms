using MediatR;
using PropFlow.Application.Rooms.Commands;

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

    private static IResult GetRooms() => Results.Ok(); // TODO: query RoomStatusBoardView
    private static IResult CreateRoom() => Results.Ok(); // TODO: CreateRoomCommand

    private static async Task<IResult> BeginCleaning(Guid id, IMediator mediator, CancellationToken ct)
    {
        await mediator.Send(new BeginCleaningCommand(id), ct);
        return Results.NoContent();
    }

    private static async Task<IResult> CompleteInspection(Guid id, IMediator mediator, CancellationToken ct)
    {
        await mediator.Send(new CompleteInspectionCommand(id), ct);
        return Results.NoContent();
    }

    private static async Task<IResult> MarkAvailable(Guid id, IMediator mediator, CancellationToken ct)
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

public sealed record BlockRequest(string? Reason);
