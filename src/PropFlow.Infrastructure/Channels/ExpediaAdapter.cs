using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using PropFlow.Domain.Channels;

namespace PropFlow.Infrastructure.Channels;

/// <summary>
/// Expedia Rapid API (EPS) adapter.
/// Spec: https://developers.expediagroup.com/docs/products/expedia-rapid
///
/// Uses JSON REST API (unlike Booking.com which uses XML).
/// Sandbox: https://test.ean.com/v3/ (requires EPS partner account)
/// In development (no credentials): requests are logged, no HTTP call is made.
/// </summary>
public sealed class ExpediaAdapter : IChannelAdapter
{
    public string ChannelCode => "expedia";

    private readonly HttpClient _http;
    private readonly ILogger<ExpediaAdapter> _logger;

    // Expedia Rapid API v3 base path
    private const string PropertiesPath = "properties";

    public ExpediaAdapter(HttpClient http, ILogger<ExpediaAdapter> logger)
    {
        _http = http;
        _logger = logger;
    }

    /// <summary>
    /// PUT /properties/{hotelId}/availability
    /// Updates room count for a single (RoomTypeCode, Date).
    /// </summary>
    public async Task PushAvailabilityAsync(
        EncryptedCredentials credentials,
        AvailabilityUpdate update,
        CancellationToken ct = default)
    {
        _logger.LogDebug(
            "[expedia] PushAvailability hotel={Hotel} room={Room} date={Date} avail={Avail}",
            credentials.HotelId, update.RoomTypeCode, update.Date, update.Available);

        if (credentials.EncryptedApiKey == "__dev__") return;

        var payload = new
        {
            room_type_id = update.RoomTypeCode,
            date = update.Date.ToString("yyyy-MM-dd"),
            available_count = update.Available,
        };

        await PutJsonAsync(
            $"{PropertiesPath}/{credentials.HotelId}/availability",
            payload, credentials, ct);
    }

    /// <summary>
    /// PUT /properties/{hotelId}/rates
    /// Updates rate for a single (RatePlanCode, RoomTypeCode, Date).
    /// </summary>
    public async Task PushRateAsync(
        EncryptedCredentials credentials,
        RateUpdate update,
        CancellationToken ct = default)
    {
        _logger.LogDebug(
            "[expedia] PushRate hotel={Hotel} room={Room} plan={Plan} date={Date} rate={Rate}",
            credentials.HotelId, update.RoomTypeCode, update.RatePlanCode, update.Date, update.BaseRate);

        if (credentials.EncryptedApiKey == "__dev__") return;

        var payload = new
        {
            rate_plan_id = update.RatePlanCode,
            room_type_id = update.RoomTypeCode,
            date = update.Date.ToString("yyyy-MM-dd"),
            rates = new[]
            {
                new {
                    currency = update.Currency,
                    nightly_rate = update.BaseRate,
                    extra_adult = update.ExtraAdultRate,
                    extra_child = update.ExtraChildRate,
                },
            },
        };

        await PutJsonAsync(
            $"{PropertiesPath}/{credentials.HotelId}/rates",
            payload, credentials, ct);
    }

    public async Task PushRestrictionAsync(
        EncryptedCredentials credentials,
        RestrictionUpdate update,
        CancellationToken ct = default)
    {
        if (credentials.EncryptedApiKey == "__dev__") return;

        var payload = new
        {
            rate_plan_id = update.RatePlanCode,
            date = update.Date.ToString("yyyy-MM-dd"),
            min_stay = update.MinStay,
            max_stay = update.MaxStay,
            closed_to_arrival = update.ClosedToArrival,
            closed_to_departure = update.ClosedToDeparture,
        };

        await PutJsonAsync(
            $"{PropertiesPath}/{credentials.HotelId}/restrictions",
            payload, credentials, ct);
    }

    /// <summary>
    /// GET /properties/{hotelId}/reservations?since={since}
    /// </summary>
    public async Task<IReadOnlyList<InboundReservation>> PullReservationsAsync(
        EncryptedCredentials credentials,
        DateTime since,
        CancellationToken ct = default)
    {
        _logger.LogDebug(
            "[expedia] PullReservations hotel={Hotel} since={Since:u}",
            credentials.HotelId, since);

        if (credentials.EncryptedApiKey == "__dev__") return [];

        var apiKey = DecryptApiKey(credentials.EncryptedApiKey);
        SetAuthHeader(apiKey);

        var url = $"{PropertiesPath}/{credentials.HotelId}/reservations" +
                  $"?since={since:yyyy-MM-ddTHH:mm:ssZ}";

        var response = await _http.GetFromJsonAsync<ExpediaReservationsResponse>(url, ct);
        return response?.Reservations?.Select(MapReservation).ToList() ?? [];
    }

    public async Task AcknowledgeReservationAsync(
        EncryptedCredentials credentials,
        string channelBookingRef,
        CancellationToken ct = default)
    {
        if (credentials.EncryptedApiKey == "__dev__") return;

        var apiKey = DecryptApiKey(credentials.EncryptedApiKey);
        SetAuthHeader(apiKey);
        await _http.DeleteAsync(
            $"{PropertiesPath}/{credentials.HotelId}/reservations/{channelBookingRef}/pending", ct);
    }

    // ─── Helpers ─────────────────────────────────────────────────────────────────

    private async Task PutJsonAsync<T>(string path, T payload, EncryptedCredentials credentials, CancellationToken ct)
    {
        var apiKey = DecryptApiKey(credentials.EncryptedApiKey);
        SetAuthHeader(apiKey);
        var response = await _http.PutAsJsonAsync(path, payload, ct);
        response.EnsureSuccessStatusCode();
    }

    private void SetAuthHeader(string apiKey) =>
        _http.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue(
                "Bearer", apiKey);

    private static string DecryptApiKey(string encryptedKey) => encryptedKey;

    private static InboundReservation MapReservation(ExpediaReservation r) => new(
        ChannelBookingRef: r.ReservationId,
        RoomTypeCode:      r.RoomTypeId,
        RatePlanCode:      r.RatePlanId,
        CheckInDate:       DateOnly.Parse(r.CheckIn),
        CheckOutDate:      DateOnly.Parse(r.CheckOut),
        Adults:            r.Adults,
        Children:          r.Children,
        TotalAmount:       r.TotalCost,
        Currency:          r.Currency,
        Guest: new InboundGuest(
            r.Guest.GivenName, r.Guest.FamilyName,
            r.Guest.Email, r.Guest.Phone, r.Guest.CountryCode),
        GuaranteeToken: r.GuaranteeToken,
        Action: r.Status switch
        {
            "cancelled" => ReservationAction.Cancelled,
            "modified"  => ReservationAction.Modified,
            _           => ReservationAction.New,
        });

    // ─── DTO contracts (Expedia Rapid API response shapes) ───────────────────────

    private sealed record ExpediaReservationsResponse(
        [property: JsonPropertyName("reservations")]
        List<ExpediaReservation>? Reservations);

    private sealed record ExpediaReservation(
        [property: JsonPropertyName("reservation_id")]    string ReservationId,
        [property: JsonPropertyName("room_type_id")]      string RoomTypeId,
        [property: JsonPropertyName("rate_plan_id")]      string RatePlanId,
        [property: JsonPropertyName("check_in")]          string CheckIn,
        [property: JsonPropertyName("check_out")]         string CheckOut,
        [property: JsonPropertyName("adults")]            int Adults,
        [property: JsonPropertyName("children")]          int Children,
        [property: JsonPropertyName("total_cost")]        decimal TotalCost,
        [property: JsonPropertyName("currency")]          string Currency,
        [property: JsonPropertyName("guest")]             ExpediaGuest Guest,
        [property: JsonPropertyName("status")]            string Status,
        [property: JsonPropertyName("guarantee_token")]   string? GuaranteeToken);

    private sealed record ExpediaGuest(
        [property: JsonPropertyName("given_name")]   string GivenName,
        [property: JsonPropertyName("family_name")]  string FamilyName,
        [property: JsonPropertyName("email")]        string? Email,
        [property: JsonPropertyName("phone")]        string? Phone,
        [property: JsonPropertyName("country_code")] string? CountryCode);
}
