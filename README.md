# propflow-pms

> Event-driven property management system with built-in channel manager — multi-tenant SaaS, CQRS/ES, .NET 9

## Architecture

```
┌─────────────────────────────────────────────────────┐
│                   PropFlow.Api                      │
│           ASP.NET Core 9 — Minimal API              │
└───────────────────┬─────────────────────────────────┘
                    │
┌───────────────────▼─────────────────────────────────┐
│               PropFlow.Application                  │
│   MediatR (CQRS) + MassTransit (Sagas)              │
└───────────────────┬─────────────────────────────────┘
                    │
┌───────────────────▼─────────────────────────────────┐
│                PropFlow.Domain                      │
│   Aggregates · State Machines · Invariants          │
│   Repository Interfaces · Domain Events             │
└─────────────────────────────────────────────────────┘
                    │
┌───────────────────▼─────────────────────────────────┐
│             PropFlow.Infrastructure                 │
│   Marten (PostgreSQL) · MassTransit · Finbuckle     │
│   Azure Service Bus · Schema-per-tenant             │
└─────────────────────────────────────────────────────┘
```

## Bounded Contexts

| Context | Responsibility |
|---------|----------------|
| `Properties` | Tenant root, timezone, currency anchor |
| `Rooms` | Physical units, 7-state housekeeping machine |
| `RoomTypes` | Sellable categories, versioned snapshots |
| `RatePlans` | Pricing grid + cancellation conditions |
| `Bookings` | Reservation lifecycle + `BookingCreationSaga` |
| `Guests` | GDPR-compliant guest profiles |
| `Inventory` | Allotment per RoomType per date — pushed to OTAs |
| `Channels` | OTA connections (Booking.com, Expedia, Airbnb), ARI sync |

## Stack

- **.NET 9** — ASP.NET Core, Minimal API
- **MediatR 12** — CQRS in-process (Commands / Queries)
- **MassTransit 8** — Sagas, Outbox, Azure Service Bus transport
- **Marten 7** — PostgreSQL document store (schema-per-tenant)
- **Finbuckle.MultiTenant** — JWT claim tenant resolution
- **Scalar** — OpenAPI docs at `/scalar`

## Key Design Decisions

- **Schema-per-tenant**: each hotel has its own PostgreSQL schema — strong isolation
- **Snapshot in Booking**: `RoomTypeSnapshot` + `RatePlanSnapshot` immutable at booking time — contractual integrity
- **Allotment explicit**: `Available = TotalRooms - Sold` per day — format all OTAs expect
- **OccupancyKind discriminant**: DayUse / Overnight / HouseUse on `Occupied` state — not separate states
- **OutOfOrder vs OutOfService**: OOO subtracts from capacity count, OOS does not
- **Single currency per Property**: immutable after first RatePlan published — RevOps consistency

## Getting Started

```bash
cp appsettings.Development.json.example appsettings.Development.json
docker-compose up -d
dotnet run --project src/PropFlow.Api
```

API docs: http://localhost:5000/scalar

## Roadmap

- [ ] Worker: night audit (NoShow + day rollover)
- [ ] IoT consumer: MQTT panel signals
- [ ] Channel adapters: Booking.com, Expedia, Airbnb
- [ ] AI Copilot: room mapping suggestion, demand forecasting
