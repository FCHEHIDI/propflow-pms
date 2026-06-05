using Microsoft.Extensions.DependencyInjection;
using PropFlow.Domain.Channels;

namespace PropFlow.Infrastructure.Channels;

public static class ChannelSetup
{
    /// <summary>
    /// Registers all channel adapters and the factory.
    /// Each adapter is registered as a named HttpClient:
    ///   "booking.com" → BookingComAdapter  (base: supply-xml.booking.com)
    ///   "expedia"     → ExpediaAdapter      (base: test.ean.com/v3/ or api.ean.com/v3/)
    /// </summary>
    public static IServiceCollection AddChannelAdapters(
        this IServiceCollection services,
        bool useSandbox = true)
    {
        // Booking.com
        services.AddHttpClient<BookingComAdapter>(client =>
        {
            var host = useSandbox
                ? "https://supply-xml.booking.com/hotels/ota/"
                : "https://supply-xml.booking.com/hotels/ota/";
            client.BaseAddress = new Uri(host);
            client.Timeout = TimeSpan.FromSeconds(30);
        });

        // Expedia Rapid API
        services.AddHttpClient<ExpediaAdapter>(client =>
        {
            var host = useSandbox
                ? "https://test.ean.com/v3/"
                : "https://api.ean.com/v3/";
            client.BaseAddress = new Uri(host);
            client.Timeout = TimeSpan.FromSeconds(30);
        });

        // Register adapters as IChannelAdapter for factory injection
        services.AddTransient<IChannelAdapter, BookingComAdapter>();
        services.AddTransient<IChannelAdapter, ExpediaAdapter>();

        // Factory resolves by ChannelCode
        services.AddSingleton<IChannelAdapterFactory, ChannelAdapterFactory>();

        return services;
    }
}
