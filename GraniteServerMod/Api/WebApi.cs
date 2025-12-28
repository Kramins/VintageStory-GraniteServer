using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using GenHTTP.Api.Content;
using GenHTTP.Api.Infrastructure;
using GenHTTP.Engine.Internal;
using GenHTTP.Modules.ApiBrowsing;
using GenHTTP.Modules.Authentication;
using GenHTTP.Modules.Authentication.ApiKey;
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
using GraniteServerMod.Data;
using GraniteServerMod.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Sieve.Services;
using Vintagestory.API.Common;
using Vintagestory.API.Server;
using Vintagestory.Common;
using Vintagestory.Server;

namespace GraniteServer.Api;

/// <summary>
/// Web API service for Vintage Story mod.
/// Provides RESTful endpoints for server administration and monitoring.
/// </summary>
public class WebApi
{
    private const ushort Port = 5000;
    private readonly ICoreServerAPI _api;
    private ServiceProvider _serviceProvider = null!;
    private GraniteDataContext _dataContext;
    private IServerHost? _host;
    private readonly GraniteServerConfig _config;
    private readonly Mod _mod;
    private readonly ILogger _logger;
    private readonly ModContainer _modContainer;

    public WebApi(ICoreServerAPI api, GraniteServerConfig config, Mod mod)
    {
        _api = api ?? throw new ArgumentNullException(nameof(api));
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _mod = mod ?? throw new ArgumentNullException(nameof(mod));
        _logger = _mod.Logger;
        _modContainer =
            mod as ModContainer
            ?? throw new ArgumentException("Mod must be a ModContainer", nameof(mod));
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
            var webClientPath = Path.Join(_modContainer.FolderPath, "wwwroot");
            _logger.Notification("[WebAPI] Starting server...");
            _logger.Notification($"[WebAPI] Serving web client from: {webClientPath}");

            var protectedControllers = Layout
                .Create()
                .AddDependentService<ServerController>("server")
                .AddDependentService<PlayerManagementController>("players")
                .AddDependentService<WorldController>("world")
                .Add(CorsPolicy.Permissive())
                .AddSwaggerUi()
                .AddScalar()
                .AddRedoc();

            var controllers = Layout
                .Create()
                .Add(protectedControllers)
                .Add(CorsPolicy.Permissive());

            // Always add authentication controller so clients can query auth settings
            controllers.AddDependentService<AuthenticationController>("auth");

            // Only add bearer auth protection if authentication is required
            if (_config.AuthenticationType.ToLower() != "none" && _config.AuthenticationType != "")
            {
                var auth = GetApiBearerAuth();
                protectedControllers.Add(auth);
            }

            var tree = ResourceTree.FromDirectory(webClientPath);
            var clientApp = StaticWebsite.From(tree);

            var app = Layout.Create().Add(clientApp).Add(["api"], controllers);

            var services = new ServiceCollection();
            services.AddSingleton<ICoreServerAPI>(_api);
            services.AddSingleton<ILogger>(_logger);

            // Configure Sieve options
            services.Configure<Sieve.Models.SieveOptions>(options =>
            {
                options.DefaultPageSize = 20;
                options.MaxPageSize = 100;
            });

            // Register application services, might need to be scoped or transient based on actual usage
            services.AddSingleton<ServerCommandService>();
            services.AddScoped<PlayerService>();
            services.AddScoped<SieveProcessor>();
            services.AddSingleton<PlayerSessionTracker>();
            services.AddSingleton<WorldService>();
            services.AddSingleton<ServerService>();
            services.AddSingleton<BasicAuthService>();
            services.AddSingleton<JwtTokenService>();
            services.AddSingleton(_config);

            // Configure database context with provider-specific derived contexts
            RegisterDatabaseContext(services);

            _serviceProvider = services.BuildServiceProvider();

            InitializeDatabase();
            RegisterPlayerSessionTrackingEvents();

            _host = Host.Create()
                .AddDependencyInjection(_serviceProvider)
                .Port(Convert.ToUInt16(_config.Port))
                .Handler(app)
                .Defaults()
                .Development()
                .Console();

            _host.StartAsync();
            _logger.Notification($"[WebAPI] Server started on port {Port}");
        }
        catch (Exception ex)
        {
            _logger.Error($"[WebAPI] Failed to start Web API: {ex.Message}\n{ex.StackTrace}");
        }
    }

    private void InitializeDatabase()
    {
        _logger.Notification("[WebAPI] Initializing database...");

        _dataContext = _serviceProvider.GetRequiredService<GraniteDataContext>();

        // _dataContext.Database.EnsureDeleted();
        _dataContext.Database.EnsureCreated();
        _dataContext.Database.Migrate();

        var ServerEntity = _dataContext.Servers.FirstOrDefault(x => x.Id == _config.ServerId);

        if (ServerEntity == null)
        {
            ServerEntity = new ServerEntity
            {
                Id = _config.ServerId,
                Name = _api.Server.Config.ServerName,
                Description = string.Empty,
            };
            _dataContext.Servers.Add(ServerEntity);
            _dataContext.SaveChanges();
            _logger.Notification("[WebAPI] Created new server entity in database.");
        }
        else
        {
            _logger.Notification("[WebAPI] Loaded existing server entity from database.");
        }

        _logger.Notification("[WebAPI] Database initialization complete.");
    }

    private void RegisterPlayerSessionTrackingEvents()
    {
        _api.Event.PlayerJoin += OnPlayerJoin;
        _api.Event.PlayerLeave += OnPlayerLeave;
    }

    private void OnPlayerLeave(IServerPlayer byPlayer)
    {
        var sessionTracker = _serviceProvider.GetService<PlayerSessionTracker>();
        sessionTracker?.OnPlayerLeave(byPlayer);
    }

    private void OnPlayerJoin(IServerPlayer byPlayer)
    {
        var sessionTracker = _serviceProvider.GetService<PlayerSessionTracker>();
        sessionTracker?.OnPlayerJoin(byPlayer);
    }

    private void RegisterDatabaseContext(ServiceCollection services)
    {
        var dbType = _config.DatabaseType?.ToLowerInvariant();

        if (dbType == "postgresql")
        {
            var connectionString =
                $"Host={_config.DatabaseHost};Port={_config.DatabasePort};Database={_config.DatabaseName};Username={_config.DatabaseUsername};Password={_config.DatabasePassword}";

            services.AddDbContext<GraniteDataContextPostgres>(options =>
                options.UseNpgsql(connectionString)
            );
            services.AddScoped<GraniteDataContext>(sp =>
                sp.GetRequiredService<GraniteDataContextPostgres>()
            );
            _logger.Notification("[WebAPI] Using PostgreSQL provider");
        }
        else if (dbType == "sqlite")
        {
            string dbPath;

            try
            {
                var basePath = _api.DataBasePath;
                var DatabaseName =
                    Path.GetExtension(_config.DatabaseName) == ".db"
                        ? _config.DatabaseName
                        : $"{_config.DatabaseName}.db";
                dbPath = Path.Combine(basePath, DatabaseName);
            }
            catch
            {
                throw new InvalidOperationException("Failed to determine SQLite database path");
            }

            Directory.CreateDirectory(Path.GetDirectoryName(dbPath) ?? AppContext.BaseDirectory);

            services.AddDbContext<GraniteDataContextSqlite>(options =>
                options.UseSqlite($"Data Source={dbPath}")
            );
            services.AddScoped<GraniteDataContext>(sp =>
                sp.GetRequiredService<GraniteDataContextSqlite>()
            );
            _logger.Notification($"[WebAPI] Using SQLite provider at: {dbPath}");
        }
        else
        {
            throw new NotSupportedException(
                $"Database type '{_config.DatabaseType}' is not supported. Use 'PostgreSQL' or 'SQLite'."
            );
        }
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

    /// <summary>
    /// Stops the Web API server gracefully.
    /// </summary>
    public void Shutdown()
    {
        _logger.Notification("[WebAPI] Stopping server...");
        if (_host != null)
        {
            try
            {
                _host.StopAsync().AsTask().Wait();
                _logger.Notification("[WebAPI] Server stopped.");
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
    }
}
