using FluentAssertions;
using PropFlow.Domain.Errors;
using PropFlow.Domain.Rooms;
using Xunit;

namespace PropFlow.Domain.Tests.Rooms;

public sealed class RoomStateMachineTests
{
    private static Room BuildAvailableRoom() => Room.Create(
        tenantId:    Guid.NewGuid(),
        propertyId:  Guid.NewGuid(),
        roomTypeId:  Guid.NewGuid(),
        roomNumber:  "101",
        floor:       1,
        squareMeters: 28m,
        bedTypeId:   Guid.NewGuid(),
        viewTypeId:  Guid.NewGuid());

    // ─── Happy paths ──────────────────────────────────────────────────

    [Fact]
    public void Occupy_FromAvailable_TransitionsToOccupied()
    {
        var room = BuildAvailableRoom();
        room.Occupy(OccupancyKind.Overnight);
        room.Status.Should().Be(RoomStatus.Occupied);
        room.Occupancy.Should().Be(OccupancyKind.Overnight);
    }

    [Fact]
    public void Vacate_FromOccupied_TransitionsToVacantDirty()
    {
        var room = BuildAvailableRoom();
        room.Occupy(OccupancyKind.Overnight);
        room.Vacate();
        room.Status.Should().Be(RoomStatus.VacantDirty);
    }

    [Fact]
    public void Vacate_ClearsOccupancyKind_Invariant()
    {
        var room = BuildAvailableRoom();
        room.Occupy(OccupancyKind.DayUse);
        room.Vacate();
        // Invariant: OccupancyKind must be null when Status != Occupied
        room.Occupancy.Should().BeNull();
    }

    [Fact]
    public void FullHousekeepingCycle_Available_Occupied_VacantDirty_OnChange_Inspected_Available()
    {
        var room = BuildAvailableRoom();

        room.Occupy(OccupancyKind.Overnight);
        room.Status.Should().Be(RoomStatus.Occupied);

        room.Vacate();
        room.Status.Should().Be(RoomStatus.VacantDirty);

        room.BeginCleaning();
        room.Status.Should().Be(RoomStatus.OnChange);

        room.CompleteInspection();
        room.Status.Should().Be(RoomStatus.Inspected);

        room.MarkAvailable();
        room.Status.Should().Be(RoomStatus.Available);
    }

    [Fact]
    public void DeclareOutOfOrder_RaisesRoomRemovedFromInventoryEvent()
    {
        var room = BuildAvailableRoom();
        room.ClearDomainEvents();

        room.DeclareOutOfOrder("Broken pipe");

        room.Status.Should().Be(RoomStatus.OutOfOrder);
        room.DomainEvents.Should().Contain(e =>
            e.GetType().Name == "RoomRemovedFromInventory");
    }

    [Fact]
    public void MarkAvailable_FromOutOfOrder_RaisesRoomRestoredToInventoryEvent()
    {
        var room = BuildAvailableRoom();
        room.DeclareOutOfOrder();
        room.ClearDomainEvents();

        room.MarkAvailable();

        room.DomainEvents.Should().Contain(e =>
            e.GetType().Name == "RoomRestoredToInventory");
    }

    // ─── Invariant violations ─────────────────────────────────────────────

    [Fact]
    public void Occupy_FromOccupied_ThrowsDomainError()
    {
        var room = BuildAvailableRoom();
        room.Occupy(OccupancyKind.Overnight);

        var act = () => room.Occupy(OccupancyKind.DayUse);
        act.Should().Throw<DomainError>()
            .Where(e => e.Kind == DomainErrorKind.InvalidState);
    }

    [Fact]
    public void Vacate_FromAvailable_ThrowsDomainError()
    {
        var room = BuildAvailableRoom();
        var act = () => room.Vacate();
        act.Should().Throw<DomainError>()
            .Where(e => e.Kind == DomainErrorKind.InvalidState);
    }

    [Fact]
    public void DeclareOutOfOrder_FromOccupied_ThrowsDomainError()
    {
        var room = BuildAvailableRoom();
        room.Occupy(OccupancyKind.Overnight);

        var act = () => room.DeclareOutOfOrder();
        act.Should().Throw<DomainError>()
            .Where(e => e.Kind == DomainErrorKind.InvalidState);
    }

    [Fact]
    public void CompleteInspection_FromVacantDirty_ThrowsDomainError()
    {
        // Must go through OnChange first
        var room = BuildAvailableRoom();
        room.Occupy(OccupancyKind.Overnight);
        room.Vacate();

        var act = () => room.CompleteInspection();
        act.Should().Throw<DomainError>()
            .Where(e => e.Kind == DomainErrorKind.InvalidState);
    }

    [Fact]
    public void Create_WithZeroSquareMeters_ThrowsValidationError()
    {
        var act = () => Room.Create(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            "101", 1, 0m, Guid.NewGuid(), Guid.NewGuid());

        act.Should().Throw<DomainError>()
            .Where(e => e.Kind == DomainErrorKind.Validation);
    }
}
