using Microsoft.EntityFrameworkCore;

namespace GraniteServerMod.Data;

public class GraniteDataContextSqlite : GraniteDataContext
{
    public GraniteDataContextSqlite(DbContextOptions<GraniteDataContextSqlite> options)
        : base(options) { }
}
