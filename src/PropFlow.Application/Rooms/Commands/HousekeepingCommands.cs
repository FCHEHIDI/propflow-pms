using MediatR;
using PropFlow.Domain.Errors;
using PropFlow.Domain.Rooms;

namespace PropFlow.Application.Rooms.Commands;

public sealed record BeginCleaningCommand(Guid RoomId) : IRequest;

public sealed class BeginCleaningCommandHandler : IRequestHandler<BeginCleaningCommand>
{
    private readonly IRoomRepository _rooms;
    public BeginCleaningCommandHandler(IRoomRepository rooms) => _rooms = rooms;

    public async Task Handle(BeginCleaningCommand cmd, CancellationToken ct)
    {
        var room = await _rooms.FindByIdAsync(cmd.RoomId, ct)
            ?? throw DomainError.NotFound($"Room {cmd.RoomId} not found.");
        room.BeginCleaning();
        await _rooms.UpdateAsync(room, ct);
    }
}

public sealed record CompleteInspectionCommand(Guid RoomId) : IRequest;

public sealed class CompleteInspectionCommandHandler : IRequestHandler<CompleteInspectionCommand>
{
    private readonly IRoomRepository _rooms;
    public CompleteInspectionCommandHandler(IRoomRepository rooms) => _rooms = rooms;

    public async Task Handle(CompleteInspectionCommand cmd, CancellationToken ct)
    {
        var room = await _rooms.FindByIdAsync(cmd.RoomId, ct)
            ?? throw DomainError.NotFound($"Room {cmd.RoomId} not found.");
        room.CompleteInspection();
        await _rooms.UpdateAsync(room, ct);
        // Inspected state will be picked up by next AssignRoom or CheckIn
    }
}

public sealed record MarkRoomAvailableCommand(Guid RoomId) : IRequest;

public sealed class MarkRoomAvailableCommandHandler : IRequestHandler<MarkRoomAvailableCommand>
{
    private readonly IRoomRepository _rooms;
    public MarkRoomAvailableCommandHandler(IRoomRepository rooms) => _rooms = rooms;

    public async Task Handle(MarkRoomAvailableCommand cmd, CancellationToken ct)
    {
        var room = await _rooms.FindByIdAsync(cmd.RoomId, ct)
            ?? throw DomainError.NotFound($"Room {cmd.RoomId} not found.");
        room.MarkAvailable();
        await _rooms.UpdateAsync(room, ct);
    }
}

public sealed record DeclareOutOfOrderCommand(Guid RoomId, string? Reason = null) : IRequest;

public sealed class DeclareOutOfOrderCommandHandler : IRequestHandler<DeclareOutOfOrderCommand>
{
    private readonly IRoomRepository _rooms;
    public DeclareOutOfOrderCommandHandler(IRoomRepository rooms) => _rooms = rooms;

    public async Task Handle(DeclareOutOfOrderCommand cmd, CancellationToken ct)
    {
        var room = await _rooms.FindByIdAsync(cmd.RoomId, ct)
            ?? throw DomainError.NotFound($"Room {cmd.RoomId} not found.");
        room.DeclareOutOfOrder(cmd.Reason);
        await _rooms.UpdateAsync(room, ct);
        // RoomRemovedFromInventory event triggers Allotment.RemoveRoom() via consumer
    }
}
