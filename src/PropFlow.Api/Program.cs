using Finbuckle.MultiTenant;
using PropFlow.Api.Endpoints;
using PropFlow.Domain.Bookings;
using PropFlow.Domain.Inventory;
using PropFlow.Domain.Rooms;
using PropFlow.Infrastructure;
using PropFlow.Infrastructure.Messaging;
using PropFlow.Infrastructure.Persistence.Repositories;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// ─── Multi-tenancy (schema-per-tenant) ─────────────────────────────────
// Tenant resolved from JWT claim "tenant_id" on each request.
builder.Services
    .AddMultiTenant<TenantInfo>()
    .WithClaimStrategy("tenant_id")
    .WithConfigurationStore();

// ─── CQRS ─────────────────────────────────────────────────────
builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssemblies(
        typeof(PropFlow.Application.AssemblyMarker).Assembly));

// ─── Persistence (Marten document store) ────────────────────────────
builder.Services.AddMartenDocumentStore(
    builder.Configuration.GetConnectionString("Postgres")
    ?? throw new InvalidOperationException("ConnectionStrings:Postgres is required."));

// ─── Messaging (MassTransit + Azure Service Bus) ──────────────────
builder.Services.AddPropFlowMessaging(
    builder.Configuration.GetConnectionString("ServiceBus")
    ?? throw new InvalidOperationException("ConnectionStrings:ServiceBus is required."));

// ─── Repositories ──────────────────────────────────────────────
builder.Services.AddScoped<IBookingRepository,    PgBookingRepository>();
builder.Services.AddScoped<IRoomRepository,       PgRoomRepository>();
builder.Services.AddScoped<IAllotmentRepository,  PgAllotmentRepository>();

// ─── OpenAPI ──────────────────────────────────────────────────
builder.Services.AddOpenApi();

var app = builder.Build();

app.UseMultiTenant();
app.MapOpenApi();
app.MapScalarApiReference();

// ─── Routes ───────────────────────────────────────────────────
app.MapGroup("/api/v1/bookings").MapBookingEndpoints();
app.MapGroup("/api/v1/rooms").MapRoomEndpoints();
app.MapGroup("/api/v1/rate-plans").MapRatePlanEndpoints();
app.MapGroup("/api/v1/channels").MapChannelEndpoints();

app.Run();
