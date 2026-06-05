using MassTransit;
using Marten;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PropFlow.Domain.Bookings;

namespace PropFlow.Infrastructure.Workers;

/// <summary>
/// Runs daily at 00:01 UTC.
/// Marks as NoShow all Confirmed/Guaranteed bookings whose CheckInDate has passed
/// without a CheckIn being recorded.
///
/// NoShow does NOT release inventory — the night has already passed.
/// Cancellation of future bookings (which DOES release inventory) is handled by BookingCancellationSaga.
///
/// Multi-tenant note: v1 uses a single session (appropriate for row-level or single-tenant deployments).
/// Production with schema-per-tenant: inject ITenantStore, iterate all tenants, open per-tenant sessions.
/// </summary>
public sealed class NightAuditWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<NightAuditWorker> _logger;

    public NightAuditWorker(IServiceScopeFactory scopeFactory, ILogger<NightAuditWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await RunAuditAsync(stoppingToken);

            var now = DateTime.UtcNow;
            var nextRun = now.Date.AddDays(1).AddMinutes(1); // 00:01 UTC next day
            var delay = nextRun - now;

            _logger.LogInformation("Night audit complete. Next run at {NextRun:u}", nextRun);
            await Task.Delay(delay, stoppingToken);
        }
    }

    private async Task RunAuditAsync(CancellationToken ct)
    {
        _logger.LogInformation("Night audit starting at {Time:u}", DateTime.UtcNow);

        using var scope = _scopeFactory.CreateScope();
        var session = scope.ServiceProvider.GetRequiredService<IDocumentSession>();
        var bus = scope.ServiceProvider.GetRequiredService<IPublishEndpoint>();

        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var candidates = await session.Query<Booking>()
            .Where(b =>
                (b.Status == BookingStatus.Confirmed || b.Status == BookingStatus.Guaranteed)
                && b.CheckInDate < today)
            .ToListAsync(ct);

        _logger.LogInformation("Night audit: {Count} no-show candidates", candidates.Count);

        var processed = 0;
        foreach (var booking in candidates)
        {
            try
            {
                booking.MarkNoShow();
                session.Store(booking);

                foreach (var @event in booking.DomainEvents)
                    await bus.Publish(@event, ct);

                booking.ClearDomainEvents();
                processed++;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Night audit: failed to mark booking {BookingId} as NoShow", booking.Id);
            }
        }

        await session.SaveChangesAsync(ct);
        _logger.LogInformation("Night audit: {Processed}/{Total} no-shows processed",
            processed, candidates.Count);
    }
}
