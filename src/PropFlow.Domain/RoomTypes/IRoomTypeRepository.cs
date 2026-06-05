namespace PropFlow.Domain.RoomTypes;

public interface IRoomTypeRepository
{
    Task<RoomType?> FindByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<RoomType>> FindActiveByPropertyAsync(Guid propertyId, CancellationToken ct = default);
    Task SaveAsync(RoomType roomType, CancellationToken ct = default);
    Task UpdateAsync(RoomType roomType, CancellationToken ct = default);
}
