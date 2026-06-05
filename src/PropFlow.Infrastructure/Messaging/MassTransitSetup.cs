using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using PropFlow.Application.Bookings.Sagas;
using PropFlow.Infrastructure.Consumers;
using PropFlow.Infrastructure.Projections;

namespace PropFlow.Infrastructure.Messaging;

public static class MassTransitSetup
{
    public static IServiceCollection AddPropFlowMessaging(
        this IServiceCollection services,
        string serviceBusConnectionString)
    {
        services.AddMassTransit(x =>
        {
            // Sagas
            x.AddSagaStateMachine<BookingCreationSaga, BookingCreationState>()
                .MartenRepository();

            // Domain event consumers
            x.AddConsumer<BookingCheckedOutConsumer>();
            x.AddConsumer<RoomRemovedFromInventoryConsumer>();
            x.AddConsumer<InventoryUpdatedConsumer>();

            // Read model projections
            x.AddConsumer<AvailabilityViewProjection>();
            x.AddConsumer<RoomStatusBoardProjection>();  // handles RoomCreated + RoomStatusChanged
            x.AddConsumer<RateSyncConsumer>();

            x.UsingAzureServiceBus((ctx, cfg) =>
            {
                cfg.Host(serviceBusConnectionString);
                cfg.ConfigureEndpoints(ctx);
            });
        });

        return services;
    }
}
