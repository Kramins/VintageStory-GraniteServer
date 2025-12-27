using System;

namespace GraniteServer;

public class GraniteServerConfig
{
    public Guid ServerId { get; set; } = Guid.NewGuid();
    public int Port { get; set; } = 5000;
    public string AuthenticationType { get; set; } = "Basic";
    public string JwtSecret { get; set; } = Guid.NewGuid().ToString();
    public int JwtExpiryMinutes { get; set; } = 60;
    public int JwtRefreshTokenExpiryMinutes { get; set; } = 1440;
    public string? Username { get; set; } = "admin";
    public string? Password { get; set; } = Guid.NewGuid().ToString();
    public string? DatabaseType { get; set; }
    public string? DatabaseHost { get; set; }
    public string? DatabaseUsername { get; set; }
    public string? DatabasePassword { get; set; }
    public int DatabasePort { get; set; } = 5432;
    public string? DatabaseName { get; set; } = "graniteserver";
    public string? SqliteFilePath { get; set; }
}
