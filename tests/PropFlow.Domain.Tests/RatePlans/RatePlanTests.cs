using FluentAssertions;
using PropFlow.Domain.Errors;
using PropFlow.Domain.RatePlans;
using Xunit;

namespace PropFlow.Domain.Tests.RatePlans;

public sealed class RatePlanTests
{
    private static RatePlan BuildDraftPlan() => RatePlan.Create(
        tenantId:             Guid.NewGuid(),
        propertyId:           Guid.NewGuid(),
        code:                 "BAR",
        name:                 "Best Available Rate",
        mealPlan:             MealPlan.RoomOnly,
        cancellationPolicyId: Guid.NewGuid(),
        isPublic:             true);

    [Fact]
    public void Publish_WithoutPrices_ThrowsValidationError()
    {
        var plan = BuildDraftPlan();
        var act = () => plan.Publish();
        act.Should().Throw<DomainError>()
            .Where(e => e.Kind == DomainErrorKind.Validation);
    }

    [Fact]
    public void Publish_WithPrice_StatusIsActive_AndRaisesEvent()
    {
        var plan = BuildDraftPlan();
        plan.SetPrice(Guid.NewGuid(), 120m);
        plan.Publish();
        plan.Status.Should().Be(RatePlanStatus.Active);
        plan.DomainEvents.Should().Contain(e =>
            e.GetType().Name == "RatePlanPublished");
    }

    [Fact]
    public void Archive_StopsSync_AndIsTerminal()
    {
        var plan = BuildDraftPlan();
        plan.SetPrice(Guid.NewGuid(), 120m);
        plan.Publish();
        plan.ClearDomainEvents();

        plan.Archive();

        plan.Status.Should().Be(RatePlanStatus.Archived);
        plan.DomainEvents.Should().Contain(e =>
            e.GetType().Name == "RatePlanArchived");
    }

    [Fact]
    public void SetPrice_NegativeRate_ThrowsValidationError()
    {
        var plan = BuildDraftPlan();
        var act = () => plan.SetPrice(Guid.NewGuid(), -10m);
        act.Should().Throw<DomainError>()
            .Where(e => e.Kind == DomainErrorKind.Validation);
    }

    [Fact]
    public void TakeSnapshot_ForUnmappedRoomType_ThrowsNotFound()
    {
        var plan = BuildDraftPlan();
        plan.SetPrice(Guid.NewGuid(), 120m);
        var act = () => plan.TakeSnapshot(Guid.NewGuid()); // unknown RoomTypeId
        act.Should().Throw<DomainError>()
            .Where(e => e.Kind == DomainErrorKind.NotFound);
    }

    [Fact]
    public void Code_IsStoredUpperCase()
    {
        var plan = RatePlan.Create(
            Guid.NewGuid(), Guid.NewGuid(),
            code: "bar", name: "Test",
            MealPlan.RoomOnly, Guid.NewGuid());

        plan.Code.Should().Be("BAR");
    }
}
