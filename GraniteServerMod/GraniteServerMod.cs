// using System;
// using System.IO;
// using System.Linq;
// using System.Reflection;
// using GraniteServer.Api.Controllers;
// using GraniteServer.Api.HostedServices;
// using GraniteServer.Api.Services;
// using GraniteServer.Common;
// using GraniteServer.Data;
// using GraniteServer.Data.Entities;
// using GraniteServer.Mod.Handlers.Commands;
// using GraniteServer.Messaging.Commands;
// using GraniteServer.Messaging.Handlers.Commands;
// using GraniteServer.Messaging.Handlers.Events;
// using Microsoft.EntityFrameworkCore;
// using Microsoft.Extensions.DependencyInjection;
// using Microsoft.Extensions.Hosting;
// using Microsoft.Extensions.Logging;
// using Sieve.Services;
// using Vintagestory.API.Common;
// using Vintagestory.API.Server;

// [assembly: ModInfo(
//     "GraniteServerMod",
//     Authors = new string[] { "Kramins" },
//     Description = "Server Administration Tools and features",
//     Version = "0.0.1"
// )]

// namespace GraniteServer
// {
//     public class GraniteServerMod : ModSystem
//     {
//         private readonly string _modConfigFileName = "graniteserverconfig.json";
//         private IHost? _host;

//         public override bool ShouldLoad(EnumAppSide side)
//         {
//             return side.IsServer();
//         }

//         private void OverrideConfigWithEnvironmentVariables(
//             GraniteServerConfig config,
//             ICoreServerAPI api
//         )
//         {
//             foreach (
//                 var property in typeof(GraniteServerConfig).GetProperties(
//                     BindingFlags.Public | BindingFlags.Instance
//                 )
//             )
//             {
//                 string envVarName = $"GS_{property.Name.ToUpper()}";
//                 string? envVarValue = Environment.GetEnvironmentVariable(envVarName);

//                 if (!string.IsNullOrEmpty(envVarValue))
//                 {
//                     try
//                     {
//                         var convertedValue = Convert.ChangeType(envVarValue, property.PropertyType);
//                         property.SetValue(config, convertedValue);
//                     }
//                     catch (Exception ex)
//                     {
//                         api.Logger.Warning(
//                             $"Failed to set property {property.Name} from environment variable {envVarName}: {ex.Message}"
//                         );
//                     }
//                 }
//             }
//         }

//         public override void StartServerSide(ICoreServerAPI api)
//         {
//             var config = api.LoadModConfig<GraniteServerConfig>(_modConfigFileName);
//             if (config == null)
//             {
//                 config = new GraniteServerConfig();
//             }

//             OverrideConfigWithEnvironmentVariables(config, api);

//             api.StoreModConfig<GraniteServerConfig>(config, _modConfigFileName);

//             _host = Host.CreateDefaultBuilder()
//                 .ConfigureLogging(logging =>
//                 {
//                     logging.AddFilter(
//                         "Microsoft.EntityFrameworkCore",
//                         Microsoft.Extensions.Logging.LogLevel.Warning
//                     );
//                     logging.AddFilter(
//                         "Microsoft.EntityFrameworkCore.Database.Command",
//                         Microsoft.Extensions.Logging.LogLevel.Warning
//                     );
//                 })
//                 .ConfigureServices(services =>
//                 {
//                     services.AddSingleton(api);
//                     services.AddSingleton<Vintagestory.API.Common.ILogger>(api.Logger);

//                     services.Configure<Sieve.Models.SieveOptions>(options =>
//                     {
//                         options.DefaultPageSize = 20;
//                         options.MaxPageSize = 100;
//                     });

//                     // services.AddSingleton<ServerCommandService>();
//                     services.AddScoped<VintageStoryProxyResolver>();
//                     services.AddScoped<PlayerService>();
//                     services.AddScoped<SieveProcessor>();
//                     // services.AddScoped<ModManagementService>();
//                     services.AddScoped<EventStreamHandler>();

//                     services.AddSingleton<WorldService>();
//                     services.AddSingleton<ServerService>();
//                     services.AddSingleton<BasicAuthService>();
//                     services.AddSingleton<JwtTokenService>();
//                     services.AddSingleton<MessageBusService>();
//                     services.AddSingleton<Mod>(Mod);
//                     services.AddSingleton(config);

//                     // Register command handlers
//                     // services.AddScoped<ICommandHandler<KickPlayerCommand>, PlayerCommandHandlers>();

//                     // Register event handlers
//                     AutoDiscoverAndRegisterEventHandlers(services, api.Logger);

//                     // Register hosted Web API service
//                     services.AddHostedService<GenHttpHostedService>();
//                     // services.AddHostedService<PlayerSessionHostedService>();
//                     // services.AddHostedService<ModSystemHostedService>();
//                     // services.AddHostedService<MessageBridgeHostedService>();

//                     RegisterDatabaseContext(services, api, config, api.Logger);
//                 })
//                 .Build();
//             try
//             {
//                 InitializeDatabase(_host.Services, config, api);

//                 _host.StartAsync();
//             }
//             catch (Exception ex)
//             {
//                 api.Logger.Error(
//                     "[Database] Initialization failed; not starting hosted services: {0}",
//                     ex.Message
//                 );
//                 try
//                 {
//                     _host?.Dispose();
//                 }
//                 catch (Exception disposeEx)
//                 {
//                     api.Logger.Warning(
//                         "[WebAPI] Error disposing host after failed DB init: {0}",
//                         disposeEx.Message
//                     );
//                 }

//                 _host = null;
//             }
//         }

//         public override void Dispose()
//         {
//             try
//             {
//                 if (_host != null)
//                 {
//                     try
//                     {
//                         _host.StopAsync().GetAwaiter().GetResult();
//                     }
//                     catch (Exception ex)
//                     {
//                         Console.Error.WriteLine($"[WebAPI] Error stopping host: {ex}");
//                     }

//                     _host = null;
//                 }
//             }
//             finally
//             {
//                 base.Dispose();
//             }
//         }



//         private void RegisterDatabaseContext(
//             IServiceCollection services,
//             ICoreServerAPI api,
//             GraniteServerConfig config,
//             Vintagestory.API.Common.ILogger logger
//         )
//         {
//             var dbType = config.DatabaseType?.ToLowerInvariant();

//             if (dbType == "postgresql")
//             {
//                 if (string.IsNullOrWhiteSpace(config.DatabaseHost))
//                 {
//                     throw new InvalidOperationException(
//                         "PostgreSQL DatabaseHost is required but not configured. "
//                             + "Set it in the config file or via GS_DATABASEHOST environment variable."
//                     );
//                 }

//                 var connectionString =
//                     $"Host={config.DatabaseHost};Port={config.DatabasePort};Database={config.DatabaseName};Username={config.DatabaseUsername};Password={config.DatabasePassword}";

//                 logger.Notification(
//                     $"[Database] Using PostgreSQL provider (Host: {config.DatabaseHost}, Port: {config.DatabasePort}, Database: {config.DatabaseName})"
//                 );

//                 services.AddDbContext<GraniteDataContextPostgres>(options =>
//                 {
//                     options.UseNpgsql(connectionString);
//                     options.LogTo(
//                         msg => logger.Warning(msg),
//                         Microsoft.Extensions.Logging.LogLevel.Warning
//                     );
//                 });
//                 services.AddScoped<GraniteDataContext>(sp =>
//                     sp.GetRequiredService<GraniteDataContextPostgres>()
//                 );
//             }
//             else if (dbType == "sqlite")
//             {
//                 string dbPath;

//                 try
//                 {
//                     var basePath =
//                         api.DataBasePath
//                         ?? throw new InvalidOperationException(
//                             "Server API database path unavailable"
//                         );
//                     var configuredName = string.IsNullOrWhiteSpace(config.DatabaseName)
//                         ? "graniteserver"
//                         : config.DatabaseName!;
//                     var databaseName =
//                         Path.GetExtension(configuredName) == ".db"
//                             ? configuredName
//                             : $"{configuredName}.db";
//                     dbPath = Path.Combine(basePath, databaseName);
//                 }
//                 catch
//                 {
//                     throw new InvalidOperationException("Failed to determine SQLite database path");
//                 }

//                 Directory.CreateDirectory(
//                     Path.GetDirectoryName(dbPath) ?? AppContext.BaseDirectory
//                 );

//                 services.AddDbContext<GraniteDataContextSqlite>(options =>
//                 {
//                     options.UseSqlite($"Data Source={dbPath}");
//                     options.LogTo(
//                         msg => logger.Warning(msg),
//                         Microsoft.Extensions.Logging.LogLevel.Warning
//                     );
//                 });
//                 services.AddScoped<GraniteDataContext>(sp =>
//                     sp.GetRequiredService<GraniteDataContextSqlite>()
//                 );
//                 logger.Notification($"[Database] Using SQLite provider at: {dbPath}");
//             }
//             else
//             {
//                 throw new NotSupportedException(
//                     $"Database type '{config.DatabaseType}' is not supported. Use 'PostgreSQL' or 'SQLite'."
//                 );
//             }
//         }

//         private void InitializeDatabase(
//             IServiceProvider serviceProvider,
//             GraniteServerConfig config,
//             ICoreServerAPI api
//         )
//         {
//             api.Logger.Notification("[Database] Initializing database...");

//             try
//             {
//                 var dataContext = serviceProvider.GetRequiredService<GraniteDataContext>();

//                 // Check for pending migrations
//                 var pendingMigrations = dataContext.Database.GetPendingMigrations().ToList();
//                 var appliedMigrations = dataContext.Database.GetAppliedMigrations().ToList();

//                 if (appliedMigrations.Any())
//                 {
//                     api.Logger.Notification(
//                         $"[Database] Current migration: {appliedMigrations.Last()}"
//                     );
//                 }
//                 else
//                 {
//                     api.Logger.Notification("[Database] No migrations applied yet (new database)");
//                 }

//                 if (pendingMigrations.Any())
//                 {
//                     api.Logger.Notification(
//                         $"[Database] Found {pendingMigrations.Count} pending migration(s):"
//                     );
//                     foreach (var migration in pendingMigrations)
//                     {
//                         api.Logger.Notification($"[Database]   - {migration}");
//                     }

//                     api.Logger.Notification("[Database] Applying migrations...");
//                     dataContext.Database.Migrate();
//                     api.Logger.Notification("[Database] Migrations applied successfully.");
//                 }
//                 else
//                 {
//                     api.Logger.Notification(
//                         "[Database] Database is up to date. No pending migrations."
//                     );
//                     // Still call Migrate to ensure database is created if it doesn't exist
//                     dataContext.Database.Migrate();
//                 }

//                 var serverEntity = dataContext.Servers.FirstOrDefault(x => x.Id == config.ServerId);

//                 if (serverEntity == null)
//                 {
//                     serverEntity = new ServerEntity
//                     {
//                         Id = config.ServerId,
//                         Name = api.Server.Config.ServerName,
//                         Description = string.Empty,
//                     };
//                     dataContext.Servers.Add(serverEntity);
//                     dataContext.SaveChanges();
//                     api.Logger.Notification("[Database] Created new server entity in database.");
//                 }
//                 else
//                 {
//                     api.Logger.Notification(
//                         "[Database] Loaded existing server entity from database."
//                     );
//                 }

//                 api.Logger.Notification("[Database] Database initialization complete.");
//             }
//             catch (System.Net.Sockets.SocketException ex)
//             {
//                 var dbType = config.DatabaseType?.ToLowerInvariant();
//                 api.Logger.Error(
//                     "[Database] Failed to connect to database server. Connection error: {0}",
//                     ex.Message
//                 );

//                 if (dbType == "postgresql")
//                 {
//                     api.Logger.Error("[Database] PostgreSQL connection failed. Please check:");
//                     api.Logger.Error($"[Database]   - Host: {config.DatabaseHost}");
//                     api.Logger.Error($"[Database]   - Port: {config.DatabasePort}");
//                     api.Logger.Error(
//                         "[Database]   - Ensure the database server is running and accessible"
//                     );
//                     api.Logger.Error(
//                         "[Database]   - Verify network connectivity and firewall settings"
//                     );
//                     api.Logger.Error(
//                         "[Database]   - Check if the hostname resolves correctly (DNS)"
//                     );
//                 }

//                 throw new InvalidOperationException(
//                     $"Database connection failed: {ex.Message}. See logs for details.",
//                     ex
//                 );
//             }
//             catch (Npgsql.NpgsqlException ex)
//             {
//                 api.Logger.Error(
//                     "[Database] PostgreSQL error during initialization: {0}",
//                     ex.Message
//                 );
//                 api.Logger.Error($"[Database] Error Code: {ex.ErrorCode}");
//                 api.Logger.Error("[Database] Check your database credentials and permissions.");
//                 throw new InvalidOperationException($"PostgreSQL error: {ex.Message}", ex);
//             }
//             catch (Microsoft.EntityFrameworkCore.DbUpdateException ex)
//             {
//                 api.Logger.Error("[Database] Failed to update database: {0}", ex.Message);
//                 if (ex.InnerException != null)
//                 {
//                     api.Logger.Error("[Database] Inner error: {0}", ex.InnerException.Message);
//                 }
//                 throw new InvalidOperationException($"Database update failed: {ex.Message}", ex);
//             }
//             catch (Exception ex)
//             {
//                 api.Logger.Error(
//                     "[Database] Unexpected error during database initialization: {0}",
//                     ex.Message
//                 );
//                 api.Logger.Error("[Database] Stack trace: {0}", ex.StackTrace);
//                 throw new InvalidOperationException(
//                     $"Database initialization failed: {ex.Message}",
//                     ex
//                 );
//             }
//         }
//     }
// }
