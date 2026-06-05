using MassTransit;
using MediatR;
using PropFlow.Application.Tenants.Commands;
using PropFlow.Domain.Tenants;

namespace PropFlow.Api.Endpoints;

public static class TenantEndpoints
{
    public static RouteGroupBuilder MapTenantEndpoints(this RouteGroupBuilder group)
    {
        // POST /api/v1/tenants  — bootstrap: hotel hasn't authenticated yet
        group.MapPost("/", OnboardTenant).AllowAnonymous();
        group.MapGet("/{identifier}", GetTenant);
        group.MapPost("/{id:guid}/suspend",   SuspendTenant);
        group.MapPost("/{id:guid}/terminate", TerminateTenant);
        return group;
    }

    private static async Task<IResult> OnboardTenant(
        OnboardTenantRequest req, ISender mediator, CancellationToken ct)
    {
        var result = await mediator.Send(
            new OnboardTenantCommand(
                req.Identifier, req.Name, req.ContactEmail, req.Plan ?? "trial"), ct);
        return Results.Created($"/api/v1/tenants/{result.Identifier}", result);
    }

    private static async Task<IResult> GetTenant(
        string identifier, ITenantRepository repo, CancellationToken ct)
    {
        var tenant = await repo.FindByIdentifierAsync(identifier, ct);
        if (tenant is null) return Results.NotFound();
        return Results.Ok(new
        {
            tenant.Id,
            tenant.Identifier,
            tenant.Name,
            tenant.ContactEmail,
            Status   = tenant.Status.ToString(),
            tenant.PlanId,
            tenant.CreatedAt,
        });
    }

    private static async Task<IResult> SuspendTenant(
        Guid id, SuspendTenantRequest req,
        ITenantRepository repo, IPublishEndpoint bus, CancellationToken ct)
    {
        var tenant = await repo.FindByIdAsync(id, ct);
        if (tenant is null) return Results.NotFound();
        tenant.Suspend(req.Reason);
        await repo.SaveAsync(tenant, ct);
        foreach (var e in tenant.DomainEvents) await bus.Publish(e, ct);
        tenant.ClearDomainEvents();
        return Results.NoContent();
    }

    private static async Task<IResult> TerminateTenant(
        Guid id, ITenantRepository repo, IPublishEndpoint bus, CancellationToken ct)
    {
        var tenant = await repo.FindByIdAsync(id, ct);
        if (tenant is null) return Results.NotFound();
        tenant.Terminate();
        await repo.SaveAsync(tenant, ct);
        foreach (var e in tenant.DomainEvents) await bus.Publish(e, ct);
        tenant.ClearDomainEvents();
        return Results.NoContent();
    }

    private sealed record OnboardTenantRequest(
        string Identifier, string Name, string ContactEmail, string? Plan);
    private sealed record SuspendTenantRequest(string Reason);
}
