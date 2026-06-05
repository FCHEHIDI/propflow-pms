using Finbuckle.MultiTenant;
using PropFlow.Api.Auth;
using PropFlow.Api.Endpoints;
using PropFlow.Application.Tenants;
using PropFlow.Domain.Bookings;
using PropFlow.Domain.Channels;
using PropFlow.Domain.Inventory;
using PropFlow.Domain.RatePlans;
using PropFlow.Domain.Rooms;
using PropFlow.Domain.Tenants;
using PropFlow.Infrastructure;
using PropFlow.Infrastructure.Channels;
using PropFlow.Infrastructure.Messaging;
using PropFlow.Infrastructure.Observability;
using PropFlow.Infrastructure.Persistence.Repositories;
using PropFlow.Infrastructure.Tenants;
using PropFlow.Infrastructure.Workers;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// ─── Multi-tenancy (schema-per-tenant) ────────────────────────────────
// Tenant resolved from JWT claim "tenant_id".
// MartenMultiTenantStore reads from the shared _admin schema at runtime.
builder.Services
    .AddMultiTenant<TenantInfo>()
    .WithClaimStrategy("tenant_id")
    .WithStore<MartenMultiTenantStore>(ServiceLifetime.Scoped);

// ─── Authentication + Authorization ───────────────────────────────
builder.Services.AddPropFlowAuth(builder.Configuration);

// ─── CQRS ─────────────────────────────────────────────────────────────────────
builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssemblies(
        typeof(PropFlow.Application.AssemblyMarker).Assembly,
        typeof(PropFlow.Infrastructure.AssemblyMarker).Assembly));

// ─── Persistence ──────────────────────────────────────────────────────────────────
builder.Services.AddMartenDocumentStore(
    builder.Configuration.GetConnectionString("Postgres")
    ?? throw new InvalidOperationException("ConnectionStrings:Postgres is required."));

// ─── Messaging ───────────────────────────────────────────────────────────────────
builder.Services.AddPropFlowMessaging(
    builder.Configuration.GetConnectionString("ServiceBus")
    ?? throw new InvalidOperationException("ConnectionStrings:ServiceBus is required."));

// ─── Channel adapters ──────────────────────────────────────────────────────────
builder.Services.AddChannelAdapters(
    useSandbox: builder.Environment.IsDevelopment());

// ─── Repositories ──────────────────────────────────────────────────────────────────
builder.Services.AddScoped<IBookingRepository,           PgBookingRepository>();
builder.Services.AddScoped<IRoomRepository,              PgRoomRepository>();
builder.Services.AddScoped<IAllotmentRepository,         PgAllotmentRepository>();
builder.Services.AddScoped<IRatePlanRepository,          PgRatePlanRepository>();
builder.Services.AddScoped<IChannelConnectionRepository, PgChannelConnectionRepository>();
builder.Services.AddScoped<ITenantRepository,            PgTenantRepository>();

// ─── Tenant provisioning ───────────────────────────────────────────────────────────
builder.Services.AddScoped<ITenantProvisioningService, TenantProvisioningService>();

// ─── Observability (OTel tracing + metrics + health checks) ─────────────────
builder.Services.AddPropFlowObservability(builder.Configuration);

// ─── Background workers ──────────────────────────────────────────────────────────────
builder.Services.AddHostedService<NightAuditWorker>();

// ─── OpenAPI ─────────────────────────────────────────────────────────────────────────
builder.Services.AddOpenApi();

var app = builder.Build();

// ─── Middleware pipeline (order matters) ───────────────────────────────
// 1. Finbuckle resolves tenant from JWT claim "tenant_id"
app.UseMultiTenant();
// 2. API key fallback for service-to-service calls (IoT panel, channel pushes)
app.UseMiddleware<ApiKeyMiddleware>();
// 3. JWT Bearer
app.UseAuthentication();
app.UseAuthorization();

// ─── OpenAPI / Scalar (no auth) ───────────────────────────────────────────────
app.MapOpenApi();
app.MapScalarApiReference();

// ─── Health checks (no auth) ─────────────────────────────────────────────────────
app.MapHealthChecks("/health/ready",
    new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
    {
        Predicate = hc => hc.Tags.Contains("ready"),
    }).AllowAnonymous();

app.MapHealthChecks("/health/live",
    new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
    {
        // Liveness = always 200 if the process is running
        Predicate = _ => false,
    }).AllowAnonymous();

// ─── API routes ──────────────────────────────────────────────────────────────────────
app.MapGroup("/api/v1/tenants")
   .MapTenantEndpoints()
   .RequireAuthorization("Manager");

app.MapGroup("/api/v1/bookings")
   .MapBookingEndpoints()
   .RequireAuthorization("Receptionist");

app.MapGroup("/api/v1/rooms")
   .MapRoomEndpoints()
   .RequireAuthorization("Housekeeper");

app.MapGroup("/api/v1/rate-plans")
   .MapRatePlanEndpoints()
   .RequireAuthorization("Manager");

app.MapGroup("/api/v1/channels")
   .MapChannelEndpoints()
   .RequireAuthorization("Manager");

// Stripe webhook is AllowAnonymous internally (signature-verified)
app.MapGroup("/api/v1/billing")
   .MapBillingEndpoints();

app.Run();
