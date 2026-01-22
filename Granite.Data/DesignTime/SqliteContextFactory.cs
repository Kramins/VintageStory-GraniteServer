using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace GraniteServer.Data.DesignTime;

public class SqliteContextFactory : IDesignTimeDbContextFactory<GraniteDataContextSqlite>
{
    public GraniteDataContextSqlite CreateDbContext(string[] args)
    {
        var builder = new DbContextOptionsBuilder<GraniteDataContextSqlite>();

        // Get the path from environment variable or use a default relative path
        var dbPath = Environment.GetEnvironmentVariable("GS_SQLITEPATH") ?? "granitesrv.db";

        // Ensure the database directory exists
        var dbDir = Path.GetDirectoryName(dbPath);
        if (!string.IsNullOrEmpty(dbDir) && !Directory.Exists(dbDir))
        {
            Directory.CreateDirectory(dbDir);
        }

        var conn = $"Data Source={dbPath}";
        builder.UseSqlite(conn);
        return new GraniteDataContextSqlite(builder.Options);
    }
}
