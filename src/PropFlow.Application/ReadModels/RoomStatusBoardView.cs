using PropFlow.Domain.Rooms;

namespace PropFlow.Application.ReadModels;

/// <summary>
/// Marten document. Updated by RoomStatusBoardProjection.
/// Id = RoomId. Created on RoomCreated, updated on RoomStatusChanged.
/// Consumed by: housekeeping tablets, IoTPanelService, front-desk dashboard.
/// </summary>
public sealed class RoomStatusBoardView
{
    /// <summary>= RoomId</summary>
    public Guid Id { get; set; }
    public Guid PropertyId { get; set; }
    public string RoomNumber { get; set; } = string.Empty;
    public int Floor { get; set; }
    public string? Wing { get; set; }
    public string? Building { get; set; }
    public RoomStatus Status { get; set; }
    /// <summary>Non-null only when Status == Occupied.</summary>
    public string? OccupancyKind { get; set; }
    public string? GuestName { get; set; }
    public DateOnly? CheckOutDate { get; set; }
    public DateTime LastChangedAt { get; set; }
    public string? AssignedHousekeeper { get; set; }
    public string? Notes { get; set; }
}
