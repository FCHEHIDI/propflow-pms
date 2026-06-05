using FluentAssertions;
using PropFlow.Domain.Errors;
using PropFlow.Domain.Inventory;
using Xunit;

namespace PropFlow.Domain.Tests.Inventory;

public sealed class AllotmentTests
{
    private static Allotment BuildAllotment(int total = 5) => Allotment.Create(
        Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
        new DateOnly(2026, 9, 1), total);

    [Fact]
    public void Decrement_ReducesAvailable()
    {
        var a = BuildAllotment(5);
        a.Decrement();
        a.Available.Should().Be(4);
        a.Sold.Should().Be(1);
    }

    [Fact]
    public void Decrement_WhenNoneLeft_ThrowsConflict()
    {
        var a = BuildAllotment(1);
        a.Decrement();

        var act = () => a.Decrement();
        act.Should().Throw<DomainError>()
            .Where(e => e.Kind == DomainErrorKind.Conflict);
    }

    [Fact]
    public void Increment_AfterDecrement_RestoresAvailable()
    {
        var a = BuildAllotment(5);
        a.Decrement();
        a.Increment();
        a.Available.Should().Be(5);
    }

    [Fact]
    public void RemoveRoom_DecrementsTotal_AndAvailable()
    {
        var a = BuildAllotment(5);
        a.RemoveRoom();
        a.TotalRooms.Should().Be(4);
        a.Available.Should().Be(4);
    }

    [Fact]
    public void RestoreRoom_IncrementsTotal()
    {
        var a = BuildAllotment(5);
        a.RemoveRoom();
        a.RestoreRoom();
        a.TotalRooms.Should().Be(5);
    }

    [Fact]
    public void Available_NeverGoesNegative()
    {
        var a = BuildAllotment(1);
        a.Decrement();
        a.RemoveRoom(); // TotalRooms = 0, Sold = 1 → Available = Max(0, -1) = 0
        a.Available.Should().Be(0);
    }

    [Fact]
    public void Decrement_RaisesInventoryUpdatedEvent()
    {
        var a = BuildAllotment(3);
        a.ClearDomainEvents();
        a.Decrement();
        a.DomainEvents.Should().Contain(e =>
            e.GetType().Name == "InventoryUpdated");
    }
}
