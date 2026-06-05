using MassTransit;
using MediatR;
using PropFlow.Domain.Errors;
using PropFlow.Domain.Tenants;

namespace PropFlow.Application.Tenants.Commands;

public sealed record OnboardTenantCommand(
    string Identifier,
    string Name,
    string ContactEmail,
    string Plan = "trial") : IRequest<OnboardTenantResult>;

public sealed record OnboardTenantResult(Guid TenantId, string Identifier);

public sealed class OnboardTenantHandler
    : IRequestHandler<OnboardTenantCommand, OnboardTenantResult>
{
    private readonly ITenantRepository _tenants;
    private readonly ITenantProvisioningService _provisioning;
    private readonly IPublishEndpoint _bus;

    public OnboardTenantHandler(
        ITenantRepository tenants,
        ITenantProvisioningService provisioning,
        IPublishEndpoint bus)
    {
        _tenants     = tenants;
        _provisioning = provisioning;
        _bus         = bus;
    }

    public async Task<OnboardTenantResult> Handle(OnboardTenantCommand cmd, CancellationToken ct)
    {
        var existing = await _tenants.FindByIdentifierAsync(cmd.Identifier, ct);
        if (existing is not null)
            throw DomainError.Conflict($"Tenant '{cmd.Identifier}' already exists.");

        var tenant = Tenant.Create(cmd.Identifier, cmd.Name, cmd.ContactEmail, cmd.Plan);

        // 1. Persist tenant record in the admin schema
        await _tenants.SaveAsync(tenant, ct);

        // 2. Provision the hotel schema in Postgres
        await _provisioning.ProvisionAsync(tenant.Identifier, tenant.Name, ct);

        // 3. Publish TenantProvisioned event
        foreach (var e in tenant.DomainEvents)
            await _bus.Publish(e, ct);
        tenant.ClearDomainEvents();

        return new OnboardTenantResult(tenant.Id, tenant.Identifier);
    }
}
