using Granite.Common.Messaging.Events;
using Granite.Server.Handlers.Events;
using GraniteServer.Handlers.Events;
using GraniteServer.Messaging.Events;
using GraniteServer.Messaging.Handlers.Events;

namespace Granite.Server.Extensions;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers all event handlers with the dependency injection container.
    /// </summary>
    public static IServiceCollection AddEventHandlers(this IServiceCollection services)
    {
        // Register PlayerEventsHandler and its event handler implementations
        services.AddScoped<PlayerEventsHandler>();
        services.AddScoped<IEventHandler<PlayerJoinedEvent>>(sp =>
            sp.GetRequiredService<PlayerEventsHandler>()
        );
        services.AddScoped<IEventHandler<PlayerLeaveEvent>>(sp =>
            sp.GetRequiredService<PlayerEventsHandler>()
        );
        services.AddScoped<IEventHandler<PlayerKickedEvent>>(sp =>
            sp.GetRequiredService<PlayerEventsHandler>()
        );
        services.AddScoped<IEventHandler<PlayerWhitelistedEvent>>(sp =>
            sp.GetRequiredService<PlayerEventsHandler>()
        );
        services.AddScoped<IEventHandler<PlayerUnwhitelistedEvent>>(sp =>
            sp.GetRequiredService<PlayerEventsHandler>()
        );

        // Register ServerMetricsEventHandler and its event handler implementation
        services.AddScoped<ServerMetricsEventHandler>();
        services.AddScoped<IEventHandler<ServerMetricsEvent>>(sp =>
            sp.GetRequiredService<ServerMetricsEventHandler>()
        );

        // Register ServerConfigEventHandler and its event handler implementation
        services.AddScoped<ServerConfigEventHandler>();
        services.AddScoped<IEventHandler<ServerConfigSyncedEvent>>(sp =>
            sp.GetRequiredService<ServerConfigEventHandler>()
        );

        // Register ServerReadyEventHandler and its event handler implementation
        services.AddScoped<ServerReadyEventHandler>();
        services.AddScoped<IEventHandler<ServerReadyEvent>>(sp =>
            sp.GetRequiredService<ServerReadyEventHandler>()
        );

        // Register CollectiblesLoadedEventHandler and its event handler implementation
        services.AddScoped<CollectiblesLoadedEventHandler>();
        services.AddScoped<IEventHandler<CollectiblesLoadedEvent>>(sp =>
            sp.GetRequiredService<CollectiblesLoadedEventHandler>()
        );

        return services;
    }
}
