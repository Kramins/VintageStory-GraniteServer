using Microsoft.EntityFrameworkCore;

namespace GraniteServer.Data;

public class GraniteDataContextSqlite : GraniteDataContext
{
    public GraniteDataContextSqlite(DbContextOptions<GraniteDataContextSqlite> options)
        : base(options) { }
}
