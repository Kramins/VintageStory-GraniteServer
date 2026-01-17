using System;

namespace Granite.Server.Configuration;

public class GraniteServerOptions
{
    public string JwtSecret { get; set; } = string.Empty;
    public int JwtExpiryMinutes { get; set; } = 60;
    public string ApiUsername { get; set; } = "admin";
    public string ApiPassword { get; set; } = string.Empty;
    public Guid ServerId { get; set; } = Guid.NewGuid();
    public int Port { get; set; } = 5000;
    public string AuthenticationType { get; set; } = "basic";
    public string ModAccessToken { get; set; } = "granite-mod-access-token-change-this";
    
    // Database configuration
    public string DatabaseType { get; set; } = "Sqlite";
    public string? DatabaseHost { get; set; }
    public int DatabasePort { get; set; } = 5432;
    public string DatabaseName { get; set; } = "graniteserver";
    public string? DatabaseUsername { get; set; }
    public string? DatabasePassword { get; set; }
    public string SqliteFilePath { get; set; } = "graniteserver.db";
}
