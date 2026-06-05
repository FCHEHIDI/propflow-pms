namespace PropFlow.Application.ReadModels;

/// <summary>Full booking detail for front-desk display.</summary>
public sealed record BookingDetailView(
    Guid BookingId,
    string Status,
    string GuestFullName,
    string RoomTypeLabel,
    string? RoomNumber,
    DateOnly CheckInDate,
    DateOnly CheckOutDate,
    int Adults,
    int Children,
    decimal TotalAmount,
    string MealPlan,
    string Source,
    string? ChannelCode,
    string? ChannelBookingRef,
    DateTime CreatedAt);
