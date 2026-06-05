using System.Security.Claims;
using Finbuckle.MultiTenant;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using PropFlow.Api.Auth;
using PropFlow.Api.Endpoints;
using PropFlow.Domain.Bookings;
using PropFlow.Domain.Inventory;
using PropFlow.Domain.Rooms;
using PropFlow.Infrastructure;
using PropFlow.Infrastructure.Channels;
using PropFlow.Infrastructure.Messaging;
using PropFlow.Infrastructure.Persistence.Repositories;
using PropFlow.Infrastructure.Workers;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// ─── Multi-tenancy (schema-per-tenant) ────────────────────────────────
// Tenant resolved from JWT claim "tenant_id" — injected by Finbuckle after JWT validation.
builder.Services
    .AddMultiTenant<TenantInfo>()
    .WithClaimStrategy("tenant_id")
    .WithConfigurationStore();

// ─── Authentication + Authorization ───────────────────────────────
builder.Services.AddPropFlowAuth(builder.Configuration);

// ─── CQRS (Application + Infrastructure handlers) ──────────────────────
builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssemblies(
        typeof(PropFlow.Application.AssemblyMarker).Assembly,
        typeof(PropFlow.Infrastructure.AssemblyMarker).Assembly));

// ─── Persistence (Marten document store) ────────────────────────────
builder.Services.AddMartenDocumentStore(
    builder.Configuration.GetConnectionString("Postgres")
    ?? throw new InvalidOperationException("ConnectionStrings:Postgres is required."));

// ─── Messaging (MassTransit + Azure Service Bus) ──────────────────
builder.Services.AddPropFlowMessaging(
    builder.Configuration.GetConnectionString("ServiceBus")
    ?? throw new InvalidOperationException("ConnectionStrings:ServiceBus is required."));

// ─── Channel adapters (Booking.com, Expedia) ───────────────────────
builder.Services.AddChannelAdapters(
    useSandbox: builder.Environment.IsDevelopment());

// ─── Repositories ──────────────────────────────────────────────
builder.Services.AddScoped<IBookingRepository,   PgBookingRepository>();
builder.Services.AddScoped<IRoomRepository,      PgRoomRepository>();
builder.Services.AddScoped<IAllotmentRepository, PgAllotmentRepository>();

// ─── Background workers ─────────────────────────────────────────
builder.Services.AddHostedService<NightAuditWorker>();

// ─── OpenAPI ──────────────────────────────────────────────────
builder.Services.AddOpenApi();

var app = builder.Build();

// ─── Middleware pipeline (order matters) ───────────────────────────
// 1. Tenant resolution (reads JWT claim — must run after auth)
app.UseMultiTenant();
// 2. API key middleware (service-to-service auth fallback)
app.UseMiddleware<ApiKeyMiddleware>();
// 3. JWT bearer auth
app.UseAuthentication();
app.UseAuthorization();
// 4. OpenAPI docs (public — no auth required)
app.MapOpenApi();
app.MapScalarApiReference();

// ─── Routes ───────────────────────────────────────────────────
app.MapGroup("/api/v1/bookings").MapBookingEndpoints().RequireAuthorization("Receptionist");
app.MapGroup("/api/v1/rooms").MapRoomEndpoints().RequireAuthorization("Housekeeper");
app.MapGroup("/api/v1/rate-plans").MapRatePlanEndpoints().RequireAuthorization("Manager");
app.MapGroup("/api/v1/channels").MapChannelEndpoints().RequireAuthorization("Manager");

app.Run();
