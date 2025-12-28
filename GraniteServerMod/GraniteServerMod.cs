using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using GraniteServer.Api;
using GraniteServer.Api.Services;
using GraniteServerMod.Data;
using GraniteServerMod.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Sieve.Services;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

[assembly: ModInfo(
    "GraniteServerMod",
    Authors = new string[] { "Kramins" },
    Description = "Server Administration Tools and features",
    Version = "0.0.1"
)]

namespace GraniteServer
{
    public class GraniteServerMod : ModSystem
    {
        private readonly string _modConfigFileName = "graniteserverconfig.json";
        private WebApi? _webApi;
        private ServiceProvider? _serviceProvider;

        public override bool ShouldLoad(EnumAppSide side)
        {
            return side.IsServer();
        }

        private void OverrideConfigWithEnvironmentVariables(
            GraniteServerConfig config,
            ICoreServerAPI api
        )
        {
            foreach (
                var property in typeof(GraniteServerConfig).GetProperties(
                    BindingFlags.Public | BindingFlags.Instance
                )
            )
            {
                string envVarName = $"GS_{property.Name.ToUpper()}";
                string? envVarValue = Environment.GetEnvironmentVariable(envVarName);

                if (!string.IsNullOrEmpty(envVarValue))
                {
                    try
                    {
                        var convertedValue = Convert.ChangeType(envVarValue, property.PropertyType);
                        property.SetValue(config, convertedValue);
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

        public override void StartServerSide(ICoreServerAPI api)
        {
            var config = api.LoadModConfig<GraniteServerConfig>(_modConfigFileName);
            if (config == null)
            {
                config = new GraniteServerConfig();
            }

            OverrideConfigWithEnvironmentVariables(config, api);

            api.StoreModConfig<GraniteServerConfig>(config, _modConfigFileName);

            _serviceProvider = BuildServiceProvider(api, config);
            InitializeDatabase(_serviceProvider, config, api);

            _webApi = new WebApi(api, config, Mod, _serviceProvider);
            _webApi.Initialize();
        }

        public override void Dispose()
        {
            _serviceProvider?.Dispose();
        }

        private ServiceProvider BuildServiceProvider(ICoreServerAPI api, GraniteServerConfig config)
        {
            var services = new ServiceCollection();

            services.AddSingleton(api);
            services.AddSingleton<ILogger>(api.Logger);

            services.Configure<Sieve.Models.SieveOptions>(options =>
            {
                options.DefaultPageSize = 20;
                options.MaxPageSize = 100;
            });

            services.AddSingleton<ServerCommandService>();
            services.AddScoped<PlayerService>();
            services.AddScoped<SieveProcessor>();
            services.AddSingleton<PlayerSessionTracker>();
            services.AddSingleton<WorldService>();
            services.AddSingleton<ServerService>();
            services.AddSingleton<BasicAuthService>();
            services.AddSingleton<JwtTokenService>();
            services.AddSingleton(config);

            RegisterDatabaseContext(services, api, config, api.Logger);

            return services.BuildServiceProvider();
        }

        private void RegisterDatabaseContext(
            IServiceCollection services,
            ICoreServerAPI api,
            GraniteServerConfig config,
            ILogger logger
        )
        {
            var dbType = config.DatabaseType?.ToLowerInvariant();

            if (dbType == "postgresql")
            {
                var connectionString =
                    $"Host={config.DatabaseHost};Port={config.DatabasePort};Database={config.DatabaseName};Username={config.DatabaseUsername};Password={config.DatabasePassword}";

                services.AddDbContext<GraniteDataContextPostgres>(options =>
                    options.UseNpgsql(connectionString)
                );
                services.AddScoped<GraniteDataContext>(sp =>
                    sp.GetRequiredService<GraniteDataContextPostgres>()
                );
                logger.Notification("[WebAPI] Using PostgreSQL provider");
            }
            else if (dbType == "sqlite")
            {
                string dbPath;

                try
                {
                    var basePath = api.DataBasePath
                        ?? throw new InvalidOperationException("Server API database path unavailable");
                    var configuredName = string.IsNullOrWhiteSpace(config.DatabaseName)
                        ? "graniteserver"
                        : config.DatabaseName!;
                    var databaseName =
                        Path.GetExtension(configuredName) == ".db"
                            ? configuredName
                            : $"{configuredName}.db";
                    dbPath = Path.Combine(basePath, databaseName);
                }
                catch
                {
                    throw new InvalidOperationException("Failed to determine SQLite database path");
                }

                Directory.CreateDirectory(
                    Path.GetDirectoryName(dbPath) ?? AppContext.BaseDirectory
                );

                services.AddDbContext<GraniteDataContextSqlite>(options =>
                    options.UseSqlite($"Data Source={dbPath}")
                );
                services.AddScoped<GraniteDataContext>(sp =>
                    sp.GetRequiredService<GraniteDataContextSqlite>()
                );
                logger.Notification($"[WebAPI] Using SQLite provider at: {dbPath}");
            }
            else
            {
                throw new NotSupportedException(
                    $"Database type '{config.DatabaseType}' is not supported. Use 'PostgreSQL' or 'SQLite'."
                );
            }
        }

        private void InitializeDatabase(
            ServiceProvider serviceProvider,
            GraniteServerConfig config,
            ICoreServerAPI api
        )
        {
            api.Logger.Notification("[WebAPI] Initializing database...");

            var dataContext = serviceProvider.GetRequiredService<GraniteDataContext>();

            dataContext.Database.EnsureCreated();
            dataContext.Database.Migrate();

            var serverEntity = dataContext.Servers.FirstOrDefault(x => x.Id == config.ServerId);

            if (serverEntity == null)
            {
                serverEntity = new ServerEntity
                {
                    Id = config.ServerId,
                    Name = api.Server.Config.ServerName,
                    Description = string.Empty,
                };
                dataContext.Servers.Add(serverEntity);
                dataContext.SaveChanges();
                api.Logger.Notification("[WebAPI] Created new server entity in database.");
            }
            else
            {
                api.Logger.Notification("[WebAPI] Loaded existing server entity from database.");
            }

            api.Logger.Notification("[WebAPI] Database initialization complete.");
        }
    }
}
