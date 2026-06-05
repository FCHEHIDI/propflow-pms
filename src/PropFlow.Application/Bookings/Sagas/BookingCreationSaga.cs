using MassTransit;
using PropFlow.Domain.Bookings;
using PropFlow.Domain.Events;
using PropFlow.Domain.Inventory;

namespace PropFlow.Application.Bookings.Sagas;

// ─── Saga State ───────────────────────────────────────────────────────────────

public sealed class BookingCreationState : SagaStateMachineInstance
{
    public Guid CorrelationId { get; set; }
    public string CurrentState { get; set; } = default!;

    public Guid TenantId { get; set; }
    public Guid PropertyId { get; set; }
    public Guid RoomTypeId { get; set; }
    public Guid RatePlanId { get; set; }
    public Guid GuestId { get; set; }
    public DateOnly CheckInDate { get; set; }
    public DateOnly CheckOutDate { get; set; }
    public int Adults { get; set; }
    public int Children { get; set; }
    public decimal TotalAmount { get; set; }
    public string Source { get; set; } = default!;
    public string? ChannelCode { get; set; }
    public string? ChannelBookingRef { get; set; }
    public string? GuaranteeToken { get; set; }
    public Guid? BookingId { get; set; }
    public string? FailureReason { get; set; }
    public int NightsBlocked { get; set; }
}

// ─── Messages ──────────────────────────────────────────────────────────────────

/// <summary>Initiates the booking creation saga. CorrelationId is caller-provided for idempotency.</summary>
public sealed record InitiateBookingCreation(
    Guid CorrelationId,
    Guid TenantId,
    Guid PropertyId,
    Guid RoomTypeId,
    Guid RatePlanId,
    Guid GuestId,
    DateOnly CheckInDate,
    DateOnly CheckOutDate,
    int Adults,
    int Children,
    decimal TotalAmount,
    string Source,
    string? ChannelCode,
    string? ChannelBookingRef,
    string? GuaranteeToken);

public sealed record InventoryBlockSucceeded(Guid CorrelationId, Guid BookingId);
public sealed record InventoryBlockFailed(Guid CorrelationId, string Reason);
public sealed record BookingCreationSucceeded(Guid CorrelationId, Guid BookingId);
public sealed record BookingCreationFailed(Guid CorrelationId, string Reason);

// ─── State Machine ───────────────────────────────────────────────────────────────

/// <summary>
/// Orchestrates booking creation across Inventory, Booking, and Channel bounded contexts.
///
/// Pipeline:
///   Initial → BlockingInventory → CreatingBooking → Final
///                              ↘ Compensating   → Final
///
/// Compensation: releases partially blocked nights if inventory check fails mid-way.
/// </summary>
public sealed class BookingCreationSaga : MassTransitStateMachine<BookingCreationState>
{
    public State BlockingInventory { get; private set; } = default!;
    public State CreatingBooking { get; private set; } = default!;
    public State Compensating { get; private set; } = default!;

    public Event<InitiateBookingCreation> BookingRequested { get; private set; } = default!;
    public Event<InventoryBlockSucceeded> InventoryBlocked { get; private set; } = default!;
    public Event<InventoryBlockFailed> InventoryFailed { get; private set; } = default!;

    public BookingCreationSaga()
    {
        InstanceState(x => x.CurrentState);

        Event(() => BookingRequested,  x => x.CorrelateById(m => m.Message.CorrelationId));
        Event(() => InventoryBlocked,  x => x.CorrelateById(m => m.Message.CorrelationId));
        Event(() => InventoryFailed,   x => x.CorrelateById(m => m.Message.CorrelationId));

        Initially(
            When(BookingRequested)
                .Then(ctx =>
                {
                    var msg = ctx.Message;
                    ctx.Saga.TenantId         = msg.TenantId;
                    ctx.Saga.PropertyId       = msg.PropertyId;
                    ctx.Saga.RoomTypeId       = msg.RoomTypeId;
                    ctx.Saga.RatePlanId       = msg.RatePlanId;
                    ctx.Saga.GuestId          = msg.GuestId;
                    ctx.Saga.CheckInDate      = msg.CheckInDate;
                    ctx.Saga.CheckOutDate     = msg.CheckOutDate;
                    ctx.Saga.Adults           = msg.Adults;
                    ctx.Saga.Children         = msg.Children;
                    ctx.Saga.TotalAmount      = msg.TotalAmount;
                    ctx.Saga.Source           = msg.Source;
                    ctx.Saga.ChannelCode      = msg.ChannelCode;
                    ctx.Saga.ChannelBookingRef = msg.ChannelBookingRef;
                    ctx.Saga.GuaranteeToken   = msg.GuaranteeToken;
                })
                // TODO: publish BlockInventoryCommand to inventory service
                .TransitionTo(BlockingInventory)
        );

        During(BlockingInventory,
            When(InventoryBlocked)
                .Then(ctx => ctx.Saga.BookingId = ctx.Message.BookingId)
                // TODO: publish NotifyChannelCommand
                .Finalize(),

            When(InventoryFailed)
                .Then(ctx => ctx.Saga.FailureReason = ctx.Message.Reason)
                // TODO: publish compensation — release any partially blocked nights
                .TransitionTo(Compensating)
                .Finalize()
        );
    }
}
