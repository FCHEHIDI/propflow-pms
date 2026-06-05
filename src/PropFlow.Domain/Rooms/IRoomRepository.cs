namespace PropFlow.Domain.Rooms;

public interface IRoomRepository
{
    Task<Room?> FindByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<Room>> FindByPropertyAsync(Guid propertyId, CancellationToken ct = default);
    Task<IReadOnlyList<Room>> FindByRoomTypeAsync(Guid roomTypeId, CancellationToken ct = default);
    Task<bool> ExistsRoomNumberAsync(Guid propertyId, string roomNumber, CancellationToken ct = default);
    Task SaveAsync(Room room, CancellationToken ct = default);
    Task UpdateAsync(Room room, CancellationToken ct = default);
}
