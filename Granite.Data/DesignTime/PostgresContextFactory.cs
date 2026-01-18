using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace GraniteServer.Data.DesignTime;

public class PostgresContextFactory : IDesignTimeDbContextFactory<GraniteDataContextPostgres>
{
    public GraniteDataContextPostgres CreateDbContext(string[] args)
    {
        var builder = new DbContextOptionsBuilder<GraniteDataContextPostgres>();

        var host = Environment.GetEnvironmentVariable("GS_DATABASEHOST") ?? "postgres";
        var portStr = Environment.GetEnvironmentVariable("GS_DATABASEPORT") ?? "5432";
        var name = Environment.GetEnvironmentVariable("GS_DATABASENAME") ?? "Granite";
        var user = Environment.GetEnvironmentVariable("GS_DATABASEUSERNAME") ?? "postgres";
        var pass = Environment.GetEnvironmentVariable("GS_DATABASEPASSWORD") ?? "postgres";

        var conn = $"Host={host};Port={portStr};Database={name};Username={user};Password={pass}";
        builder.UseNpgsql(conn);
        return new GraniteDataContextPostgres(builder.Options);
    }
}
