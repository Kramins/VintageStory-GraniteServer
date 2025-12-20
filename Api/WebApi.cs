using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using GenHTTP.Api.Infrastructure;
using GenHTTP.Engine.Internal;
using GenHTTP.Modules.ApiBrowsing;
using GenHTTP.Modules.Controllers;
using GenHTTP.Modules.DependencyInjection;
using GenHTTP.Modules.IO;
using GenHTTP.Modules.Layouting;
using GenHTTP.Modules.Practices;
using GenHTTP.Modules.Security;
using GenHTTP.Modules.StaticWebsites;
using GenHTTP.Modules.Webservices;
using GraniteServer.Api.Controllers;
using GraniteServer.Api.Services;
using Microsoft.Extensions.DependencyInjection;
using Vintagestory.API.Common;
using Vintagestory.API.Server;
using Vintagestory.Common;

namespace GraniteServer.Api;

/// <summary>
/// Web API service for Vintage Story mod.
/// Provides RESTful endpoints for server administration and monitoring.
/// </summary>
public class WebApi
{
    private const ushort Port = 5000;
    private readonly ICoreServerAPI _api;
    private IServerHost? _host;
    private readonly GraniteServerConfig _config;

    public WebApi(ICoreServerAPI api, GraniteServerConfig config)
    {
        _api = api ?? throw new ArgumentNullException(nameof(api));
        _config = config ?? throw new ArgumentNullException(nameof(config));
    }

    public void Initialize()
    {
        _api.Event.ServerRunPhase(EnumServerRunPhase.RunGame, RunGame);
        _api.Event.ServerRunPhase(EnumServerRunPhase.Shutdown, Shutdown);
    }

    /// <summary>
    /// Starts the Web API server asynchronously.
    /// </summary>
    public void RunGame()
    {
        try
        {
            var graniteServerMod =
                _api.ModLoader.Mods.FirstOrDefault(m => m.Info.ModID == "graniteserver")
                as ModContainer;
            if (graniteServerMod == null)
            {
                _api.Logger.Error("[WebAPI] Could not find GraniteServer mod container.");
                return;
            }

            var webClientPath = Path.Join(graniteServerMod.FolderPath, "wwwroot");
            _api.Logger.Notification("[WebAPI] Starting server...");
            _api.Logger.Notification($"[WebAPI] Serving web client from: {webClientPath}");

            var controllers = Layout
                .Create()
                .AddDependentController<ServerController>("server")
                .AddDependentService<PlayerManagementController>("players")
                .AddDependentService<WorldController>("world")
                .Add(CorsPolicy.Permissive())
                .AddSwaggerUi()
                .AddScalar()
                .AddRedoc();

            var tree = ResourceTree.FromDirectory(webClientPath);
            var clientApp = StaticWebsite.From(tree);

            var app = Layout
                .Create()
                // .Add(["client"], clientApp)
                .Add(["api"], controllers);

            var services = new ServiceCollection();
            services.AddSingleton<ICoreServerAPI>(_api);
            services.AddSingleton<ServerCommandService>();
            services.AddSingleton<PlayerService>();
            services.AddSingleton<WorldService>();

            _host = Host.Create()
                .AddDependencyInjection(services.BuildServiceProvider())
                .Port(Convert.ToUInt16(_config.Port))
                .Handler(app)
                .Defaults()
                .Development()
                .Console();

            _host.StartAsync();
            _api.Logger.Notification($"[WebAPI] Server started on port {Port}");
        }
        catch (Exception ex)
        {
            _api.Logger.Error($"[WebAPI] Failed to start Web API: {ex.Message}\n{ex.StackTrace}");
        }
    }

    /// <summary>
    /// Stops the Web API server gracefully.
    /// </summary>
    public void Shutdown()
    {
        _api.Logger.Notification("[WebAPI] Stopping server...");
        if (_host != null)
        {
            try
            {
                _host.StopAsync().AsTask().Wait();
                _api.Logger.Notification("[WebAPI] Server stopped.");
            }
            catch (Exception ex)
            {
                _api.Logger.Error($"[WebAPI] Error stopping Web API: {ex}");
            }
            finally
            {
                _host = null;
            }
        }
    }
}
