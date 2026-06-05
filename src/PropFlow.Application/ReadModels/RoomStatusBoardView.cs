using PropFlow.Domain.Rooms;

namespace PropFlow.Application.ReadModels;

/// <summary>
/// Live projection consumed by housekeeping tablets and IoTPanelService.
/// Updated on every RoomStatusChanged event.
/// </summary>
public sealed record RoomStatusBoardView
{
    public Guid RoomId { get; init; }
    public Guid PropertyId { get; init; }
    public string RoomNumber { get; init; } = default!;
    public int Floor { get; init; }
    public string? Wing { get; init; }
    public string? Building { get; init; }
    public RoomStatus Status { get; init; }
    public string? OccupancyKind { get; init; }
    public string? GuestName { get; init; }
    public DateTime? CheckOutTime { get; init; }
    public DateTime LastChangedAt { get; init; }
    public string? AssignedHousekeeper { get; init; }
    public string? Notes { get; init; }
}
