using System;
using System.Threading;
using System.Threading.Tasks;
using GraniteServer.Api.Models;
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
        private readonly EventBusService _eventBus;
        private readonly ICoreServerAPI _api;

        public EventBridgeHostedService(
            ILogger logger,
            EventBusService eventBus,
            ICoreServerAPI api
        )
        {
            _logger = logger;
            _eventBus = eventBus;
            _api = api;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            try
            {
                _logger.Notification("[EventBridge] Starting event bridge...");

                // Subscribe to player session events (login, disconnect, etc.)
                _api.Event.PlayerJoin += OnPlayerJoin;
                _api.Event.PlayerLeave += OnPlayerLeave;

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

                // Unsubscribe from all events
                _api.Event.PlayerJoin -= OnPlayerJoin;
                _api.Event.PlayerLeave -= OnPlayerLeave;

                _logger.Notification("[EventBridge] Event bridge stopped");
            }
            catch (Exception ex)
            {
                _logger.Error($"[EventBridge] Error stopping event bridge: {ex.Message}");
            }

            return Task.CompletedTask;
        }

        private void OnPlayerJoin(IServerPlayer player)
        {
            try
            {
                var @event = new EventDto
                {
                    EventType = "player.join",
                    Source = "server",
                    Data = new
                    {
                        playerName = player.PlayerName,
                        playerId = player.PlayerUID,
                        joinTime = DateTime.UtcNow,
                    },
                };

                _eventBus.Publish(@event);
            }
            catch (Exception ex)
            {
                _logger.Warning($"[EventBridge] Error publishing player.join event: {ex.Message}");
            }
        }

        private void OnPlayerLeave(IServerPlayer player)
        {
            try
            {
                var @event = new EventDto
                {
                    EventType = "player.leave",
                    Source = "server",
                    Data = new
                    {
                        playerName = player.PlayerName,
                        playerId = player.PlayerUID,
                        leaveTime = DateTime.UtcNow,
                    },
                };

                _eventBus.Publish(@event);
            }
            catch (Exception ex)
            {
                _logger.Warning($"[EventBridge] Error publishing player.leave event: {ex.Message}");
            }
        }
    }
}
