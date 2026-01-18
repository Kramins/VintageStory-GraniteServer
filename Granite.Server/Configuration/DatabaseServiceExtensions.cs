using GraniteServer.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Granite.Server.Configuration;

public static class DatabaseServiceExtensions
{
    public static IServiceCollection AddGraniteDatabase(
        this IServiceCollection services,
        IConfiguration configuration,
        ILogger? logger = null
    )
    {
        var options = configuration.GetSection("GraniteServer").Get<GraniteServerOptions>()
            ?? throw new InvalidOperationException("GraniteServer configuration section is missing");

        // Apply environment variable overrides before configuring database
        options.ApplyEnvironmentVariables();

        var databaseType = options.DatabaseType?.ToLower() ?? "sqlite";

        if (databaseType == "postgres" || databaseType == "postgresql")
        {
            logger?.LogInformation(
                "Configuring PostgreSQL database: {Host}:{Port}/{Database}",
                options.DatabaseHost,
                options.DatabasePort,
                options.DatabaseName
            );
            RegisterPostgres(services, options);
        }
        else if (databaseType == "sqlite")
        {
            logger?.LogInformation("Configuring SQLite database: {FilePath}", options.SqliteFilePath);
            RegisterSqlite(services, options);
        }
        else
        {
            throw new InvalidOperationException(
                $"Unsupported database type: {options.DatabaseType}. Supported types are 'Postgres' or 'Sqlite'."
            );
        }

        return services;
    }

    private static void RegisterPostgres(IServiceCollection services, GraniteServerOptions options)
    {
        if (string.IsNullOrEmpty(options.DatabaseHost))
        {
            throw new InvalidOperationException(
                "DatabaseHost is required when using PostgreSQL. Set GraniteServer:DatabaseHost in configuration."
            );
        }

        var connectionString =
            $"Host={options.DatabaseHost};Port={options.DatabasePort};"
            + $"Database={options.DatabaseName};Username={options.DatabaseUsername};"
            + $"Password={options.DatabasePassword}";

        services.AddDbContext<GraniteDataContextPostgres>(dbOptions =>
        {
            dbOptions.UseNpgsql(connectionString);
            dbOptions.EnableSensitiveDataLogging();
        });

        services.AddScoped<GraniteDataContext>(sp =>
            sp.GetRequiredService<GraniteDataContextPostgres>()
        );
    }

    private static void RegisterSqlite(IServiceCollection services, GraniteServerOptions options)
    {
        var dbPath = options.SqliteFilePath;
        
        // Ensure .db extension
        if (!dbPath.EndsWith(".db", StringComparison.OrdinalIgnoreCase))
        {
            dbPath += ".db";
        }

        // Create directory if it doesn't exist
        var directory = Path.GetDirectoryName(dbPath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var connectionString = $"Data Source={dbPath}";

        services.AddDbContext<GraniteDataContextSqlite>(dbOptions =>
        {
            dbOptions.UseSqlite(connectionString);
            dbOptions.EnableSensitiveDataLogging();
        });

        services.AddScoped<GraniteDataContext>(sp =>
            sp.GetRequiredService<GraniteDataContextSqlite>()
        );
    }
}
