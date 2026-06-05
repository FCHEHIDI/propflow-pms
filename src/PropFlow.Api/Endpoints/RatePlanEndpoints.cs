using MediatR;

namespace PropFlow.Api.Endpoints;

public static class RatePlanEndpoints
{
    public static RouteGroupBuilder MapRatePlanEndpoints(this RouteGroupBuilder group)
    {
        group.MapPost("/",                      CreateRatePlan).WithName("CreateRatePlan");
        group.MapGet("/{id:guid}",              GetRatePlan).WithName("GetRatePlan");
        group.MapPut("/{id:guid}/prices",       SetPrices).WithName("SetRatePlanPrices");
        group.MapPost("/{id:guid}/publish",     PublishRatePlan).WithName("PublishRatePlan");
        group.MapPost("/{id:guid}/suspend",     SuspendRatePlan).WithName("SuspendRatePlan");
        group.MapDelete("/{id:guid}",           ArchiveRatePlan).WithName("ArchiveRatePlan");
        return group;
    }

    private static IResult CreateRatePlan()              => Results.Ok();        // TODO
    private static IResult GetRatePlan(Guid id)          => Results.Ok();        // TODO
    private static IResult SetPrices(Guid id)            => Results.NoContent(); // TODO
    private static IResult PublishRatePlan(Guid id)      => Results.NoContent(); // TODO
    private static IResult SuspendRatePlan(Guid id)      => Results.NoContent(); // TODO
    private static IResult ArchiveRatePlan(Guid id)      => Results.NoContent(); // TODO
}
