using System;
using GenHTTP.Modules.Controllers;
using GenHTTP.Modules.Webservices;

namespace GraniteServer.Api;

/// <summary>
/// World manipulation and configuration controller
/// </summary>
public class WorldManagementController
{
    /// <summary>
    /// Get world configuration
    /// Linked to: /worldconfig [key] [value] command
    /// </summary>
    public object GetWorldConfig(string? key = null)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Set world configuration
    /// Linked to: /worldconfig <key> <value> command
    /// </summary>
    public object SetWorldConfig(string key, string value)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Set world time
    /// Linked to: /time command
    /// </summary>
    public object SetTime(double time)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Set time speed
    /// Linked to: /time command
    /// </summary>
    public object SetTimeSpeed(float speed)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Get current weather
    /// Linked to: /weather command
    /// </summary>
    public object GetWeather()
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Set weather
    /// Linked to: /weather command
    /// </summary>
    public object SetWeather(string weatherType)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Create a backup of the current save game
    /// Linked to: /genbackup [filename] command
    /// </summary>
    public object CreateBackup(string? filename = null)
    {
        throw new NotImplementedException();
    }
}
