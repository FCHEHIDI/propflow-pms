using Finbuckle.MultiTenant;
using PropFlow.Api.Endpoints;
using PropFlow.Domain.Bookings;
using PropFlow.Domain.Inventory;
using PropFlow.Domain.Rooms;
using PropFlow.Infrastructure;
using PropFlow.Infrastructure.Messaging;
using PropFlow.Infrastructure.Persistence.Repositories;
using PropFlow.Infrastructure.Workers;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// ─── Multi-tenancy (schema-per-tenant) ────────────────────────────────
builder.Services
    .AddMultiTenant<TenantInfo>()
    .WithClaimStrategy("tenant_id")
    .WithConfigurationStore();

// ─── CQRS (Application + Infrastructure handlers) ──────────────────────
// Application: command handlers, saga messages
// Infrastructure: query handlers (require IQuerySession from Marten)
builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssemblies(
        typeof(PropFlow.Application.AssemblyMarker).Assembly,
        typeof(PropFlow.Infrastructure.AssemblyMarker).Assembly));

// ─── Persistence (Marten document store) ────────────────────────────
// Marten registers IDocumentSession (write) and IQuerySession (read) automatically.
builder.Services.AddMartenDocumentStore(
    builder.Configuration.GetConnectionString("Postgres")
    ?? throw new InvalidOperationException("ConnectionStrings:Postgres is required."));

// ─── Messaging (MassTransit + Azure Service Bus) ──────────────────
builder.Services.AddPropFlowMessaging(
    builder.Configuration.GetConnectionString("ServiceBus")
    ?? throw new InvalidOperationException("ConnectionStrings:ServiceBus is required."));

// ─── Repositories ──────────────────────────────────────────────
builder.Services.AddScoped<IBookingRepository,   PgBookingRepository>();
builder.Services.AddScoped<IRoomRepository,      PgRoomRepository>();
builder.Services.AddScoped<IAllotmentRepository, PgAllotmentRepository>();

// ─── Background workers ─────────────────────────────────────────
builder.Services.AddHostedService<NightAuditWorker>();

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
