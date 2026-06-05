using MassTransit;
using MediatR;
using PropFlow.Domain.Errors;
using PropFlow.Domain.RatePlans;

namespace PropFlow.Application.RatePlans.Commands;

// ─── Create ──────────────────────────────────────────────────────────────────
public sealed record CreateRatePlanCommand(
    Guid TenantId,
    Guid PropertyId,
    string Code,
    string Name,
    MealPlan MealPlan,
    Guid CancellationPolicyId,
    bool IsPublic = true) : IRequest<Guid>;

public sealed class CreateRatePlanHandler : IRequestHandler<CreateRatePlanCommand, Guid>
{
    private readonly IRatePlanRepository _repo;
    public CreateRatePlanHandler(IRatePlanRepository repo) => _repo = repo;

    public async Task<Guid> Handle(CreateRatePlanCommand cmd, CancellationToken ct)
    {
        var plan = RatePlan.Create(
            cmd.TenantId, cmd.PropertyId,
            cmd.Code, cmd.Name, cmd.MealPlan,
            cmd.CancellationPolicyId, cmd.IsPublic);
        await _repo.SaveAsync(plan, ct);
        return plan.Id;
    }
}

// ─── Set Price ────────────────────────────────────────────────────────────────
public sealed record SetRatePlanPriceCommand(
    Guid RatePlanId,
    Guid RoomTypeId,
    decimal BaseRate,
    decimal? ExtraAdult = null,
    decimal? ExtraChild = null) : IRequest;

public sealed class SetRatePlanPriceHandler : IRequestHandler<SetRatePlanPriceCommand>
{
    private readonly IRatePlanRepository _repo;
    public SetRatePlanPriceHandler(IRatePlanRepository repo) => _repo = repo;

    public async Task Handle(SetRatePlanPriceCommand cmd, CancellationToken ct)
    {
        var plan = await _repo.GetAsync(cmd.RatePlanId, ct)
            ?? throw DomainError.NotFound($"RatePlan {cmd.RatePlanId} not found.");
        plan.SetPrice(cmd.RoomTypeId, cmd.BaseRate, cmd.ExtraAdult, cmd.ExtraChild);
        await _repo.SaveAsync(plan, ct);
    }
}

// ─── Publish ───────────────────────────────────────────────────────────────────
public sealed record PublishRatePlanCommand(Guid RatePlanId) : IRequest;

public sealed class PublishRatePlanHandler : IRequestHandler<PublishRatePlanCommand>
{
    private readonly IRatePlanRepository _repo;
    private readonly IPublishEndpoint _bus;

    public PublishRatePlanHandler(IRatePlanRepository repo, IPublishEndpoint bus)
    {
        _repo = repo;
        _bus  = bus;
    }

    public async Task Handle(PublishRatePlanCommand cmd, CancellationToken ct)
    {
        var plan = await _repo.GetAsync(cmd.RatePlanId, ct)
            ?? throw DomainError.NotFound($"RatePlan {cmd.RatePlanId} not found.");
        plan.Publish();
        await _repo.SaveAsync(plan, ct);
        foreach (var e in plan.DomainEvents) await _bus.Publish(e, ct);
        plan.ClearDomainEvents();
    }
}

// ─── Archive ───────────────────────────────────────────────────────────────────
public sealed record ArchiveRatePlanCommand(Guid RatePlanId) : IRequest;

public sealed class ArchiveRatePlanHandler : IRequestHandler<ArchiveRatePlanCommand>
{
    private readonly IRatePlanRepository _repo;
    private readonly IPublishEndpoint _bus;

    public ArchiveRatePlanHandler(IRatePlanRepository repo, IPublishEndpoint bus)
    {
        _repo = repo;
        _bus  = bus;
    }

    public async Task Handle(ArchiveRatePlanCommand cmd, CancellationToken ct)
    {
        var plan = await _repo.GetAsync(cmd.RatePlanId, ct)
            ?? throw DomainError.NotFound($"RatePlan {cmd.RatePlanId} not found.");
        plan.Archive();
        await _repo.SaveAsync(plan, ct);
        foreach (var e in plan.DomainEvents) await _bus.Publish(e, ct);
        plan.ClearDomainEvents();
    }
}
