using System;
using System.Diagnostics.Tracing;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using GenHTTP.Api.Content;
using GenHTTP.Api.Infrastructure;
using GenHTTP.Modules.DependencyInjection;
using GenHTTP.Modules.ErrorHandling;
using GenHTTP.Modules.IO;
using GenHTTP.Modules.Layouting;
using GenHTTP.Modules.Practices;
using GenHTTP.Modules.Security;
using GenHTTP.Modules.ServerSentEvents;
using GenHTTP.Modules.StaticWebsites;
using GraniteServer.Api.Controllers;
using GraniteServer.Api.Handlers;
using GraniteServer.Api.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Vintagestory.API.Common;
using Vintagestory.API.Server;
using Vintagestory.Common;

namespace GraniteServer.Api.HostedServices
{
    public class GenHttpHostedService : IHostedService
    {
        private readonly ILogger _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly ICoreServerAPI _api;
        private readonly GraniteServerConfig _config;
        private readonly ModContainer _modContainer;
        private IServerHost? _host;

        public GenHttpHostedService(IServiceProvider serviceProvider, ILogger logger)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
            _api =
                serviceProvider.GetService<ICoreServerAPI>()
                ?? throw new ArgumentNullException(nameof(ICoreServerAPI));
            _config =
                _serviceProvider.GetService<GraniteServerConfig>()
                ?? throw new ArgumentNullException(nameof(GraniteServerConfig));
            var mod = _serviceProvider.GetService<Mod>();

            _modContainer =
                mod as ModContainer
                ?? throw new ArgumentException("Mod must be a ModContainer", nameof(mod));
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                _logger.Notification("[WebAPI] Start cancelled via cancellation token");
                return;
            }

            try
            {
                // Initialize and start the web api host. Initialization itself starts the host.
                InitializeWebApi();
                _api.Event.ServerRunPhase(EnumServerRunPhase.RunGame, RunGame);
                _api.Event.ServerRunPhase(EnumServerRunPhase.Shutdown, Shutdown);
            }
            catch (OperationCanceledException)
            {
                _logger.Notification("[WebAPI] Start aborted by cancellation");
            }
            catch (Exception ex)
            {
                _logger.Error($"[WebAPI] Error during StartAsync: {ex}");
            }
        }

        private void Shutdown()
        {
            _ = StopAsync(CancellationToken.None);
        }

        private void RunGame()
        {
            _ = _host.StartAsync();
            _logger.Notification($"[WebAPI] Server started on port {_config.Port}");
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.Notification("[WebAPI] Stopping hosted WebAPI...");

            if (_host == null)
            {
                _logger.Notification("[WebAPI] No host to stop");
                return;
            }

            try
            {
                var stopTask = _host.StopAsync().AsTask();

                if (cancellationToken.CanBeCanceled)
                {
                    var completed = await Task.WhenAny(
                        stopTask,
                        Task.Delay(Timeout.Infinite, cancellationToken)
                    );
                    if (completed != stopTask)
                    {
                        _logger.Notification("[WebAPI] Stop cancelled by cancellation token");
                        return;
                    }
                }
                else
                {
                    await stopTask;
                }

                _logger.Notification("[WebAPI] Server stopped.");
            }
            catch (OperationCanceledException)
            {
                _logger.Notification("[WebAPI] Stop aborted by cancellation");
            }
            catch (Exception ex)
            {
                _logger.Error($"[WebAPI] Error stopping Web API: {ex}");
            }
            finally
            {
                _host = null;
            }
        }

        private void InitializeWebApi()
        {
            try
            {
                var webClientPath = Path.Join(_modContainer.FolderPath, "wwwroot");
                _logger.Notification("[WebAPI] Starting server...");
                _logger.Notification($"[WebAPI] Serving web client from: {webClientPath}");

                var errorHandling = ErrorHandler.From(new JsonApiErrorMapper());

                var sse = GenHTTP
                    .Modules.ServerSentEvents.EventSource.Create()
                    .Generator(StreamEventsAsync);

                var protectedControllers = Layout
                    .Create()
                    .AddDependentService<ServerController>("server")
                    .AddDependentService<PlayerManagementController>("players")
                    .AddDependentService<WorldController>("world")
                    .AddDependentService<ModManagementController>("mods")
                    .Add("events", sse)
                    .Add(CorsPolicy.Permissive());

                var controllers = Layout
                    .Create()
                    .Add(errorHandling)
                    .Add(protectedControllers)
                    .Add(CorsPolicy.Permissive());

                // Always add authentication controller so clients can query auth settings
                controllers.AddDependentService<AuthenticationController>("auth");

                // Only add bearer auth protection if authentication is required
                if (
                    _config.AuthenticationType.ToLower() != "none"
                    && _config.AuthenticationType != ""
                )
                {
                    var auth = GetApiBearerAuth();
                    protectedControllers.Add(auth);
                }

                var tree = ResourceTree.FromDirectory(webClientPath);
                var clientApp = StaticWebsite.From(tree);

                var app = Layout.Create().Add(clientApp).Add(["api"], controllers);

                //RegisterPlayerSessionTrackingEvents();

                _host = GenHTTP
                    .Engine.Internal.Host.Create()
                    .AddDependencyInjection(_serviceProvider)
                    .Port(Convert.ToUInt16(_config.Port))
                    .Handler(app)
                    .Defaults()
                    .Development()
                    .Companion(
                        new WithModLogging(_logger, _serviceProvider.GetService<JwtTokenService>()!)
                    );
            }
            catch (Exception ex)
            {
                _logger.Error($"[WebAPI] Failed to start Web API: {ex.Message}\n{ex.StackTrace}");
            }
        }

        private async ValueTask StreamEventsAsync(IEventConnection connection)
        {
            using var scope = _serviceProvider.CreateScope();
            var messageBusHandler = scope.ServiceProvider.GetRequiredService<EventStreamHandler>();
            await messageBusHandler.StreamEventsAsync(connection);
        }

        private IConcernBuilder GetApiBearerAuth()
        {
            /// NOTE: There is a missing feature in GenHTTP where the BearerAuthentication
            /// modules does not have built-in support for custom Signing Keys.
            /// Will result in a 500 `Unable to fetch signing issuer signing keys`
            /// Will have to submit a feature request or PR to add this functionality.
            return CustomBearerAuthentication
                .CustomBearerAuthentication.Create()
                .Issuer("GraniteServer")
                .Audience("GraniteServerClient")
                .Validation(
                    (token) =>
                    {
                        return Task.CompletedTask;
                    }
                )
                .UserMapping(
                    (request, token) =>
                    {
                        var user = new GenHTTP.Modules.Authentication.Basic.BasicAuthenticationUser(
                            token.Subject,
                            Array.Empty<string>()
                        );
                        return new(user);
                    }
                )
                .KeyResolver(
                    (token) =>
                    {
                        var jwtTokenService = _serviceProvider.GetService<JwtTokenService>();

                        if (jwtTokenService == null)
                        {
                            throw new InvalidOperationException("JwtTokenService not available");
                        }

                        var keys = jwtTokenService.GetSigningKeys();

                        return new(keys);
                    }
                );
        }
    }
}
