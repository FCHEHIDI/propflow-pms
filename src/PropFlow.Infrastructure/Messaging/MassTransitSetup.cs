using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using PropFlow.Application.Bookings.Sagas;
using PropFlow.Infrastructure.Consumers;

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
                .MartenRepository(); // Saga state persisted in Marten

            // Consumers
            x.AddConsumer<BookingCheckedOutConsumer>();
            x.AddConsumer<RoomRemovedFromInventoryConsumer>();
            x.AddConsumer<InventoryUpdatedConsumer>();

            x.UsingAzureServiceBus((ctx, cfg) =>
            {
                cfg.Host(serviceBusConnectionString);
                cfg.ConfigureEndpoints(ctx);
            });
        });

        return services;
    }
}
