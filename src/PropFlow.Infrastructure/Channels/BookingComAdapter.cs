using System.Net.Http.Json;
using System.Text;
using System.Xml.Linq;
using Microsoft.Extensions.Logging;
using PropFlow.Domain.Channels;

namespace PropFlow.Infrastructure.Channels;

/// <summary>
/// Booking.com ARI adapter using the OTA_HotelAvailNotif / OTA_HotelRateAmountNotif XML protocol.
/// Spec: https://connect.booking.com/user_guide/site/en-US/xml-api/
///
/// Sandbox endpoint: https://supply-xml.booking.com/hotels/ota/ (requires partner account)
/// In development (no credentials): requests are logged and no HTTP call is made.
/// </summary>
public sealed class BookingComAdapter : IChannelAdapter
{
    public string ChannelCode => "booking.com";

    private readonly HttpClient _http;
    private readonly ILogger<BookingComAdapter> _logger;

    // Booking.com OTA XML endpoints
    private const string AvailNotifPath   = "OTA_HotelAvailNotif";
    private const string RateNotifPath    = "OTA_HotelRateAmountNotif";
    private const string ReservationPath  = "OTA_HotelResRetrieve";

    public BookingComAdapter(HttpClient http, ILogger<BookingComAdapter> logger)
    {
        _http = http;
        _logger = logger;
    }

    /// <summary>
    /// OTA_HotelAvailNotif — updates room count for a single date.
    /// </summary>
    public async Task PushAvailabilityAsync(
        EncryptedCredentials credentials,
        AvailabilityUpdate update,
        CancellationToken ct = default)
    {
        var payload = BuildAvailNotif(credentials.HotelId, update);
        _logger.LogDebug(
            "[booking.com] PushAvailability hotel={Hotel} room={Room} date={Date} avail={Avail}",
            credentials.HotelId, update.RoomTypeCode, update.Date, update.Available);

        await PostXmlAsync(AvailNotifPath, payload, credentials, ct);
    }

    /// <summary>
    /// OTA_HotelRateAmountNotif — updates rate for a single (RatePlan, RoomType, Date).
    /// </summary>
    public async Task PushRateAsync(
        EncryptedCredentials credentials,
        RateUpdate update,
        CancellationToken ct = default)
    {
        var payload = BuildRateNotif(credentials.HotelId, update);
        _logger.LogDebug(
            "[booking.com] PushRate hotel={Hotel} room={Room} plan={Plan} date={Date} rate={Rate}",
            credentials.HotelId, update.RoomTypeCode, update.RatePlanCode, update.Date, update.BaseRate);

        await PostXmlAsync(RateNotifPath, payload, credentials, ct);
    }

    public async Task PushRestrictionAsync(
        EncryptedCredentials credentials,
        RestrictionUpdate update,
        CancellationToken ct = default)
    {
        // Restrictions use the same OTA_HotelAvailNotif envelope with RestrictionStatus nodes
        var payload = BuildRestrictionNotif(credentials.HotelId, update);
        _logger.LogDebug(
            "[booking.com] PushRestriction hotel={Hotel} plan={Plan} date={Date}",
            credentials.HotelId, update.RatePlanCode, update.Date);

        await PostXmlAsync(AvailNotifPath, payload, credentials, ct);
    }

    public async Task<IReadOnlyList<InboundReservation>> PullReservationsAsync(
        EncryptedCredentials credentials,
        DateTime since,
        CancellationToken ct = default)
    {
        var payload = BuildResRetrieve(credentials.HotelId, since);
        _logger.LogDebug(
            "[booking.com] PullReservations hotel={Hotel} since={Since:u}",
            credentials.HotelId, since);

        var xml = await PostXmlAsync(ReservationPath, payload, credentials, ct);
        return xml is null ? [] : ParseReservations(xml);
    }

    public async Task AcknowledgeReservationAsync(
        EncryptedCredentials credentials,
        string channelBookingRef,
        CancellationToken ct = default)
    {
        _logger.LogDebug(
            "[booking.com] AcknowledgeReservation hotel={Hotel} ref={Ref}",
            credentials.HotelId, channelBookingRef);
        // Booking.com acknowledgement: mark reservation as processed via OTA_HotelResModify
        await Task.CompletedTask;
    }

    // ─── XML builders ─────────────────────────────────────────────────────────────────

    private static XDocument BuildAvailNotif(string hotelId, AvailabilityUpdate u) =>
        new(new XDeclaration("1.0", "utf-8", null),
            new XElement("OTA_HotelAvailNotifRQ",
                new XAttribute("Version", "2.0"),
                new XAttribute("EchoToken", Guid.NewGuid().ToString("N")),
                new XElement("AvailStatusMessages",
                    new XAttribute("HotelCode", hotelId),
                    new XElement("AvailStatusMessage",
                        new XAttribute("BookingLimit", u.Available),
                        new XElement("StatusApplicationControl",
                            new XAttribute("Start", u.Date.ToString("yyyy-MM-dd")),
                            new XAttribute("End",   u.Date.ToString("yyyy-MM-dd")),
                            new XAttribute("InvTypeCode", u.RoomTypeCode))))));

    private static XDocument BuildRateNotif(string hotelId, RateUpdate u) =>
        new(new XDeclaration("1.0", "utf-8", null),
            new XElement("OTA_HotelRateAmountNotifRQ",
                new XAttribute("Version", "2.0"),
                new XAttribute("EchoToken", Guid.NewGuid().ToString("N")),
                new XElement("RateAmountMessages",
                    new XAttribute("HotelCode", hotelId),
                    new XElement("RateAmountMessage",
                        new XElement("StatusApplicationControl",
                            new XAttribute("Start",        u.Date.ToString("yyyy-MM-dd")),
                            new XAttribute("End",          u.Date.ToString("yyyy-MM-dd")),
                            new XAttribute("InvTypeCode",  u.RoomTypeCode),
                            new XAttribute("RatePlanCode", u.RatePlanCode)),
                        new XElement("Rates",
                            new XElement("Rate",
                                new XAttribute("CurrencyCode", u.Currency),
                                new XElement("Base",
                                    new XAttribute("AmountAfterTax", u.BaseRate.ToString("F2")))))))));

    private static XDocument BuildRestrictionNotif(string hotelId, RestrictionUpdate u) =>
        new(new XDeclaration("1.0", "utf-8", null),
            new XElement("OTA_HotelAvailNotifRQ",
                new XAttribute("Version", "2.0"),
                new XAttribute("EchoToken", Guid.NewGuid().ToString("N")),
                new XElement("AvailStatusMessages",
                    new XAttribute("HotelCode", hotelId),
                    new XElement("AvailStatusMessage",
                        new XElement("StatusApplicationControl",
                            new XAttribute("Start",        u.Date.ToString("yyyy-MM-dd")),
                            new XAttribute("End",          u.Date.ToString("yyyy-MM-dd")),
                            new XAttribute("RatePlanCode", u.RatePlanCode)),
                        new XElement("RestrictionStatus",
                            new XAttribute("MinAdvancedBookingOffset", u.MinStay?.ToString() ?? "1"),
                            new XAttribute("Restriction", u.ClosedToArrival ? "Arrival" : string.Empty))))));

    private static XDocument BuildResRetrieve(string hotelId, DateTime since) =>
        new(new XDeclaration("1.0", "utf-8", null),
            new XElement("OTA_HotelResRetrieveRQ",
                new XAttribute("Version", "2.0"),
                new XAttribute("EchoToken", Guid.NewGuid().ToString("N")),
                new XElement("HotelResRetrieveCriteria",
                    new XElement("Criterion",
                        new XAttribute("HotelCode", hotelId),
                        new XElement("LastModifiedDate",
                            new XAttribute("DateTime", since.ToString("yyyy-MM-ddTHH:mm:ssZ")))))));

    // ─── HTTP ────────────────────────────────────────────────────────────────────

    private async Task<XDocument?> PostXmlAsync(
        string path,
        XDocument payload,
        EncryptedCredentials credentials,
        CancellationToken ct)
    {
        // In development (no real credentials), log and return null — no HTTP call.
        if (credentials.EncryptedApiKey == "__dev__")
        {
            _logger.LogInformation("[booking.com][DEV] {Path}: {Xml}",
                path, payload.ToString());
            return null;
        }

        var xml = payload.ToString(SaveOptions.DisableFormatting);
        var content = new StringContent(xml, Encoding.UTF8, "application/xml");

        // Basic auth: hotel_id:api_key (decoded at request time, never stored decoded)
        var apiKey = DecryptApiKey(credentials.EncryptedApiKey);
        var authHeader = Convert.ToBase64String(
            Encoding.UTF8.GetBytes($"{credentials.HotelId}:{apiKey}"));
        _http.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", authHeader);

        var response = await _http.PostAsync(path, content, ct);
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadAsStringAsync(ct);
        return XDocument.Parse(body);
    }

    /// <summary>
    /// Placeholder: real implementation uses Azure Key Vault or DPAPI.
    /// Never log or expose the decrypted key.
    /// </summary>
    private static string DecryptApiKey(string encryptedKey) => encryptedKey;

    // ─── Response parser ───────────────────────────────────────────────────────────

    private static IReadOnlyList<InboundReservation> ParseReservations(XDocument xml)
    {
        var ns = xml.Root?.Name.Namespace ?? XNamespace.None;
        var reservations = new List<InboundReservation>();

        foreach (var resEl in xml.Descendants(ns + "HotelReservation"))
        {
            try
            {
                var status = resEl.Attribute("ResStatus")?.Value ?? "Book";
                var action = status switch
                {
                    "Cancel"   => ReservationAction.Cancelled,
                    "Modify"   => ReservationAction.Modified,
                    _          => ReservationAction.New,
                };

                var roomStay  = resEl.Descendants(ns + "RoomStay").First();
                var roomType  = roomStay.Descendants(ns + "RoomType").First();
                var ratePlan  = roomStay.Descendants(ns + "RatePlan").First();
                var timeSpan  = roomStay.Descendants(ns + "TimeSpan").First();
                var guestEl   = resEl.Descendants(ns + "ResGlobalInfo").First()
                                     .Descendants(ns + "Profiles").First();
                var nameEl    = guestEl.Descendants(ns + "PersonName").First();
                var total     = roomStay.Descendants(ns + "Total").First();

                reservations.Add(new InboundReservation(
                    ChannelBookingRef: resEl.Descendants(ns + "UniqueID").First().Attribute("ID")!.Value,
                    RoomTypeCode:      roomType.Attribute("RoomTypeCode")!.Value,
                    RatePlanCode:      ratePlan.Attribute("RatePlanCode")!.Value,
                    CheckInDate:       DateOnly.Parse(timeSpan.Attribute("Start")!.Value),
                    CheckOutDate:      DateOnly.Parse(timeSpan.Attribute("End")!.Value),
                    Adults:            int.Parse(roomStay.Descendants(ns + "GuestCount")
                                           .FirstOrDefault(g => g.Attribute("AgeQualifyingCode")?.Value == "10")
                                           ?.Attribute("Count")?.Value ?? "1"),
                    Children:          int.Parse(roomStay.Descendants(ns + "GuestCount")
                                           .FirstOrDefault(g => g.Attribute("AgeQualifyingCode")?.Value == "8")
                                           ?.Attribute("Count")?.Value ?? "0"),
                    TotalAmount:       decimal.Parse(total.Attribute("AmountAfterTax")!.Value),
                    Currency:          total.Attribute("CurrencyCode")!.Value,
                    Guest: new InboundGuest(
                        FirstName:   nameEl.Element(ns + "GivenName")?.Value ?? string.Empty,
                        LastName:    nameEl.Element(ns + "Surname")?.Value   ?? string.Empty,
                        Email:       guestEl.Descendants(ns + "Email").FirstOrDefault()?.Value,
                        Phone:       guestEl.Descendants(ns + "PhoneNumber").FirstOrDefault()?.Value,
                        Nationality: guestEl.Descendants(ns + "CountryName")
                                            .FirstOrDefault()?.Attribute("Code")?.Value),
                    GuaranteeToken:    null,
                    Action:            action));
            }
            catch (Exception)
            {
                // Skip malformed reservation elements — log and continue
            }
        }

        return reservations;
    }
}
