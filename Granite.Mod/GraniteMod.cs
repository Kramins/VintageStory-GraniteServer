using System.Reflection;
using GraniteServer.HostedServices;
using GraniteServer.Messaging.Commands;
using GraniteServer.Messaging.Handlers.Commands;
using GraniteServer.Messaging.Handlers.Events;
using GraniteServer.Mod.Handlers.Commands;
using GraniteServer.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

[assembly: ModInfo(
    "GraniteServerMod",
    Authors = new string[] { "Kramins" },
    Description = "Server Administration Tools and features",
    Version = "0.0.1"
)]

namespace GraniteServer.Mod;

public class GraniteMod : ModSystem
{
    private IHost? _host;
    private GraniteModConfig? _config;
    private readonly string _configFileName = "graniteConfig.json";

    public override void StartServerSide(ICoreServerAPI api)
    {
        api.Logger.Notification("GraniteServer Mod starting server side.");

        _config = api.LoadModConfig<GraniteModConfig>(_configFileName);
        if (_config == null)
        {
            _config = new GraniteModConfig();
        }

        OverrideConfigWithEnvironmentVariables(_config, api);

        api.StoreModConfig<GraniteModConfig>(_config, _configFileName);
        _host = Host.CreateDefaultBuilder()
            .ConfigureServices(services =>
            {
                services.AddSingleton(api);
                services.AddSingleton<Vintagestory.API.Common.ILogger>(api.Logger);

                services.AddSingleton<ServerCommandService>();
                services.AddSingleton<ClientMessageBusService>();
                services.AddSingleton<SignalRConnectionState>();
                services.AddSingleton(_config);

                services.AddSingleton<Vintagestory.API.Common.Mod>(Mod);

                // Register command handlers
                services.AddScoped<ICommandHandler<KickPlayerCommand>, PlayerCommandHandlers>();

                // Register event handlers
                AutoDiscoverAndRegisterEventHandlers(services, api.Logger);

                // Register hosted services
                services.AddHostedService<PlayerSessionHostedService>();
                services.AddHostedService<MessageBridgeHostedService>();
                services.AddHostedService<SignalRClientHostedService>();
                services.AddHostedService<ServerMetricsHostedService>();
                services.AddHostedService<ServerReadyHostedService>();
            })
            .Build();

        // Start the host so hosted services run
        _ = _host.StartAsync();
    }

    private void AutoDiscoverAndRegisterEventHandlers(
        IServiceCollection services,
        Vintagestory.API.Common.ILogger logger
    )
    {
        var assembly = Assembly.GetExecutingAssembly();
        var eventHandlerType = typeof(IEventHandler<>);
        var commandHandlerType = typeof(ICommandHandler<>);

        logger?.Notification(
            $"[Handlers] Scanning assembly {assembly.GetName().Name} for event and command handlers..."
        );

        // Find all types that implement IEventHandler<> or ICommandHandler<>
        var handlerTypes = assembly
            .GetTypes()
            .Where(t =>
                !t.IsAbstract
                && !t.IsInterface
                && t.GetInterfaces()
                    .Any(i =>
                        (i.IsGenericType && i.GetGenericTypeDefinition() == eventHandlerType)
                        || (i.IsGenericType && i.GetGenericTypeDefinition() == commandHandlerType)
                    )
            )
            .ToList();

        logger?.Notification($"[Handlers] Found {handlerTypes.Count} handler type(s).");

        foreach (var handlerType in handlerTypes)
        {
            logger?.Notification($"[Handlers] Processing handler type: {handlerType.FullName}");

            // Get all event and command handler interfaces
            var handlerInterfaces = handlerType
                .GetInterfaces()
                .Where(i =>
                    i.IsGenericType
                    && (i.GetGenericTypeDefinition() == eventHandlerType
                        || i.GetGenericTypeDefinition() == commandHandlerType)
                )
                .ToList();

            foreach (var handlerInterface in handlerInterfaces)
            {
                try
                {
                    services.AddScoped(handlerInterface, handlerType);
                    var interfaceName = handlerInterface.GetGenericTypeDefinition().Name;
                    logger?.Notification(
                        $"[Handlers] Registered {interfaceName}<{string.Join(", ", handlerInterface.GetGenericArguments().Select(t => t.Name))}> -> {handlerType.FullName}"
                    );
                }
                catch (Exception ex)
                {
                    logger?.Warning(
                        $"[Handlers] Failed to register handler {handlerType.FullName} for interface {handlerInterface.FullName}: {ex.Message}"
                    );
                }
            }
        }
    }

    private void OverrideConfigWithEnvironmentVariables(GraniteModConfig config, ICoreServerAPI api)
    {
        foreach (
            var property in typeof(GraniteModConfig).GetProperties(
                BindingFlags.Public | BindingFlags.Instance
            )
        )
        {
            string envVarName = $"GS_{property.Name.ToUpper()}";
            string? envValue = Environment.GetEnvironmentVariable(envVarName);

            if (!string.IsNullOrEmpty(envValue))
            {
                try
                {
                    // Handle different property types
                    if (property.PropertyType == typeof(string))
                    {
                        property.SetValue(config, envValue);
                    }
                    else if (property.PropertyType == typeof(int))
                    {
                        if (int.TryParse(envValue, out var intValue))
                            property.SetValue(config, intValue);
                    }
                    else if (property.PropertyType == typeof(Guid))
                    {
                        if (Guid.TryParse(envValue, out var guidValue))
                            property.SetValue(config, guidValue);
                    }
                    else if (property.PropertyType == typeof(string))
                    {
                        property.SetValue(config, envValue);
                    }
                    else if (Nullable.GetUnderlyingType(property.PropertyType) != null)
                    {
                        // Handle nullable types
                        var underlyingType = Nullable.GetUnderlyingType(property.PropertyType);
                        if (underlyingType == typeof(string))
                        {
                            property.SetValue(config, envValue);
                        }
                    }
                }
                catch (Exception ex)
                {
                    api.Logger.Warning(
                        $"Failed to set property {property.Name} from environment variable {envVarName}: {ex.Message}"
                    );
                }
            }
        }
    }

    public override void Dispose()
    {
        _host?.StopAsync().Wait();
        _host?.Dispose();
        base.Dispose();
    }
}
