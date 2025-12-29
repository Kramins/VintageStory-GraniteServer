using Microsoft.EntityFrameworkCore;

namespace GraniteServerMod.Data;

public class GraniteDataContextPostgres : GraniteDataContext
{
    public GraniteDataContextPostgres(DbContextOptions<GraniteDataContextPostgres> options)
        : base(options) { }
}
