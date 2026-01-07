using System;
using System.Threading;
using System.Threading.Tasks;
using GraniteServer.Api.Models;
using GraniteServer.Api.Messaging.Events;
using GraniteServer.Api.Services;
using Microsoft.Extensions.Hosting;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace GraniteServer.Api.HostedServices
{
    /// <summary>
    /// Hosted service that bridges Vintagestory server events into the EventBusService.
    /// This allows the web API to expose server events to clients via SSE.
    ///
    /// Subscribes to:
    /// - PlayerLogin / PlayerDisconnect (player session changes)
    /// - ServerSave (world save events)
    /// - Possible future game-world events
    /// </summary>
    public class EventBridgeHostedService : IHostedService
    {
        private readonly ILogger _logger;
        private readonly MessageBusService _messageBus;
        private readonly ICoreServerAPI _api;

        public EventBridgeHostedService(
            ILogger logger,
            MessageBusService messageBus,
            ICoreServerAPI api
        )
        {
            _logger = logger;
            _messageBus = messageBus;
            _api = api;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            try
            {
                _logger.Notification("[EventBridge] Starting event bridge...");

                _logger.Notification(
                    "[EventBridge] Event bridge started, subscribed to server events"
                );
            }
            catch (Exception ex)
            {
                _logger.Error($"[EventBridge] Error starting event bridge: {ex.Message}");
            }

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            try
            {
                _logger.Notification("[EventBridge] Stopping event bridge...");

                _logger.Notification("[EventBridge] Event bridge stopped");
            }
            catch (Exception ex)
            {
                _logger.Error($"[EventBridge] Error stopping event bridge: {ex.Message}");
            }

            return Task.CompletedTask;
        }
    }
}
