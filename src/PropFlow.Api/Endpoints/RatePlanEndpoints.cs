using MediatR;
using PropFlow.Application.RatePlans.Commands;
using PropFlow.Domain.RatePlans;

namespace PropFlow.Api.Endpoints;

public static class RatePlanEndpoints
{
    public static RouteGroupBuilder MapRatePlanEndpoints(this RouteGroupBuilder group)
    {
        group.MapPost("/",                           CreateRatePlan);
        group.MapGet("/",                            GetByProperty);
        group.MapPut("/{id:guid}/prices/{roomTypeId:guid}", SetPrice);
        group.MapPost("/{id:guid}/publish",          Publish);
        group.MapPost("/{id:guid}/archive",          Archive);
        return group;
    }

    private static async Task<IResult> CreateRatePlan(
        CreateRatePlanRequest req, ISender mediator, HttpContext ctx, CancellationToken ct)
    {
        var id = await mediator.Send(new CreateRatePlanCommand(
            GetTenantId(ctx), req.PropertyId,
            req.Code, req.Name, req.MealPlan,
            req.CancellationPolicyId, req.IsPublic), ct);
        return Results.Created($"/api/v1/rate-plans/{id}", new { Id = id });
    }

    private static async Task<IResult> GetByProperty(
        Guid propertyId, IRatePlanRepository repo, CancellationToken ct)
    {
        var plans = await repo.GetByPropertyAsync(propertyId, ct);
        return Results.Ok(plans.Select(p => new
        {
            p.Id,
            p.Code,
            p.Name,
            Status  = p.Status.ToString(),
            MealPlan = p.MealPlan.ToString(),
            p.IsPublic,
            PriceCount = p.Prices.Count,
        }));
    }

    private static async Task<IResult> SetPrice(
        Guid id, Guid roomTypeId, SetPriceRequest req,
        ISender mediator, CancellationToken ct)
    {
        await mediator.Send(
            new SetRatePlanPriceCommand(id, roomTypeId, req.BaseRate, req.ExtraAdult, req.ExtraChild), ct);
        return Results.NoContent();
    }

    private static async Task<IResult> Publish(
        Guid id, ISender mediator, CancellationToken ct)
    {
        await mediator.Send(new PublishRatePlanCommand(id), ct);
        return Results.NoContent();
    }

    private static async Task<IResult> Archive(
        Guid id, ISender mediator, CancellationToken ct)
    {
        await mediator.Send(new ArchiveRatePlanCommand(id), ct);
        return Results.NoContent();
    }

    private static Guid GetTenantId(HttpContext ctx) =>
        Guid.TryParse(ctx.User.FindFirst("tenant_id")?.Value, out var id) ? id : Guid.Empty;

    private sealed record CreateRatePlanRequest(
        Guid PropertyId, string Code, string Name,
        MealPlan MealPlan, Guid CancellationPolicyId,
        bool IsPublic = true);
    private sealed record SetPriceRequest(
        decimal BaseRate,
        decimal? ExtraAdult = null,
        decimal? ExtraChild = null);
}
