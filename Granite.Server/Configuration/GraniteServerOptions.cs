using System;

namespace Granite.Server.Configuration;

public class GraniteServerOptions
{
    public string JwtSecret { get; set; } = string.Empty;
    public int JwtExpiryMinutes { get; set; } = 60;
    
    /// <summary>
    /// Admin username for seeding the initial admin user account on startup.
    /// This is only used for initial seeding - after the admin user is created,
    /// additional users can be registered via the API.
    /// </summary>
    public string Username { get; set; } = "admin";
    
    /// <summary>
    /// Admin password for seeding the initial admin user account on startup.
    /// This is only used for initial seeding - after the admin user is created,
    /// additional users can be registered via the API.
    /// Defaults to a strong password if not configured.
    /// </summary>
    public string Password { get; set; } = $"Admin{Guid.NewGuid():N}1!";
    
    public int Port { get; set; } = 5000;
    
    /// <summary>
    /// Authentication type identifier returned to clients.
    /// With ASP.NET Identity integration, this defaults to "identity".
    /// </summary>
    public string AuthenticationType { get; set; } = "identity";

    // Database configuration
    public string DatabaseType { get; set; } = "Sqlite";
    public string? DatabaseHost { get; set; }
    public int DatabasePort { get; set; } = 5432;
    public string DatabaseName { get; set; } = "graniteserver";
    public string? DatabaseUsername { get; set; }
    public string? DatabasePassword { get; set; }
    public string SqliteFilePath { get; set; } = "graniteserver.db";

    // Granite Mod Server configuration
    public Guid GraniteModServerId { get; set; }
    public string? GraniteModToken { get; set; }

    // Vintage Story Auth Server configuration
    public string? AuthServerUrl { get; set; }

    public void ApplyEnvironmentVariables()
    {
        // Use reflection to automatically apply environment variables with GS_ prefix
        var properties = GetType()
            .GetProperties(
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance
            );

        foreach (var property in properties)
        {
            if (!property.CanWrite)
                continue;

            // Construct environment variable name: GS_ + PROPERTYNAME
            var envVarName = $"GS_{property.Name.ToUpperInvariant()}";
            var envValue = Environment.GetEnvironmentVariable(envVarName);

            if (string.IsNullOrWhiteSpace(envValue))
                continue;

            try
            {
                // Handle different property types
                if (property.PropertyType == typeof(string))
                {
                    property.SetValue(this, envValue);
                }
                else if (property.PropertyType == typeof(int))
                {
                    if (int.TryParse(envValue, out var intValue))
                        property.SetValue(this, intValue);
                }
                else if (property.PropertyType == typeof(Guid))
                {
                    if (Guid.TryParse(envValue, out var guidValue))
                        property.SetValue(this, guidValue);
                }
                else if (property.PropertyType == typeof(string))
                {
                    property.SetValue(this, envValue);
                }
                else if (Nullable.GetUnderlyingType(property.PropertyType) != null)
                {
                    // Handle nullable types
                    var underlyingType = Nullable.GetUnderlyingType(property.PropertyType);
                    if (underlyingType == typeof(string))
                    {
                        property.SetValue(this, envValue);
                    }
                }
            }
            catch
            {
                // Ignore conversion errors and continue
            }
        }
    }
}
