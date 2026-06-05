using MediatR;
using PropFlow.Application.Bookings.Commands;
using PropFlow.Domain.Bookings;

namespace PropFlow.Api.Endpoints;

public static class BookingEndpoints
{
    public static RouteGroupBuilder MapBookingEndpoints(this RouteGroupBuilder group)
    {
        group.MapPost("/",                        CreateBooking).WithName("CreateBooking");
        group.MapGet("/{id:guid}",               GetBooking).WithName("GetBooking");
        group.MapPost("/{id:guid}/check-in",     CheckIn).WithName("CheckIn");
        group.MapPost("/{id:guid}/check-out",    CheckOut).WithName("CheckOut");
        group.MapPost("/{id:guid}/cancel",       CancelBooking).WithName("CancelBooking");
        return group;
    }

    private static async Task<IResult> CreateBooking(
        CreateBookingRequest req,
        IMediator mediator,
        CancellationToken ct)
    {
        var cmd = new CreateBookingCommand(
            req.TenantId, req.PropertyId, req.RoomTypeId, req.RatePlanId,
            req.GuestId, req.CheckInDate, req.CheckOutDate,
            req.Adults, req.Children, req.Source, req.TotalAmount,
            req.ChannelCode, req.ChannelBookingRef, req.GuaranteeToken, req.Notes);

        var bookingId = await mediator.Send(cmd, ct);
        return Results.Created($"/api/v1/bookings/{bookingId}", new { bookingId });
    }

    private static IResult GetBooking(Guid id, IMediator mediator)
    {
        // TODO: query BookingDetailView read model
        return Results.Ok();
    }

    private static async Task<IResult> CheckIn(
        Guid id, CheckInRequest req, IMediator mediator, CancellationToken ct)
    {
        await mediator.Send(new CheckInCommand(id, req.RoomId), ct);
        return Results.NoContent();
    }

    private static async Task<IResult> CheckOut(Guid id, IMediator mediator, CancellationToken ct)
    {
        await mediator.Send(new CheckOutCommand(id), ct);
        return Results.NoContent();
    }

    private static async Task<IResult> CancelBooking(
        Guid id, CancelRequest req, IMediator mediator, CancellationToken ct)
    {
        await mediator.Send(new CancelBookingCommand(id, req.Reason), ct);
        return Results.NoContent();
    }
}

public sealed record CreateBookingRequest(
    Guid TenantId, Guid PropertyId, Guid RoomTypeId, Guid RatePlanId,
    Guid GuestId, DateOnly CheckInDate, DateOnly CheckOutDate,
    int Adults, int Children, BookingSource Source, decimal TotalAmount,
    string? ChannelCode = null, string? ChannelBookingRef = null,
    string? GuaranteeToken = null, string? Notes = null);

public sealed record CheckInRequest(Guid RoomId);
public sealed record CancelRequest(string Reason);
