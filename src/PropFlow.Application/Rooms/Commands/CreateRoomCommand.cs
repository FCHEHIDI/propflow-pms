using MediatR;
using PropFlow.Domain.Errors;
using PropFlow.Domain.Rooms;

namespace PropFlow.Application.Rooms.Commands;

public sealed record CreateRoomCommand(
    Guid TenantId,
    Guid PropertyId,
    Guid RoomTypeId,
    string RoomNumber,
    int Floor,
    decimal SquareMeters,
    Guid BedTypeId,
    Guid ViewTypeId,
    string? Wing = null,
    string? Building = null) : IRequest<Guid>;

public sealed class CreateRoomCommandHandler : IRequestHandler<CreateRoomCommand, Guid>
{
    private readonly IRoomRepository _rooms;

    public CreateRoomCommandHandler(IRoomRepository rooms) => _rooms = rooms;

    public async Task<Guid> Handle(CreateRoomCommand cmd, CancellationToken ct)
    {
        var exists = await _rooms.ExistsRoomNumberAsync(cmd.PropertyId, cmd.RoomNumber, ct);
        if (exists)
            throw DomainError.Conflict(
                $"Room number '{cmd.RoomNumber}' already exists in property {cmd.PropertyId}.");

        var room = Room.Create(
            cmd.TenantId, cmd.PropertyId, cmd.RoomTypeId,
            cmd.RoomNumber, cmd.Floor, cmd.SquareMeters,
            cmd.BedTypeId, cmd.ViewTypeId,
            cmd.Wing, cmd.Building);

        // SaveAsync dispatches RoomCreated → RoomStatusBoardProjection creates initial view
        await _rooms.SaveAsync(room, ct);
        return room.Id;
    }
}
