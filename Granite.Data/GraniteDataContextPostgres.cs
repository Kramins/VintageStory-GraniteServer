using Microsoft.EntityFrameworkCore;

namespace GraniteServer.Data;

public class GraniteDataContextPostgres : GraniteDataContext
{
    public GraniteDataContextPostgres(DbContextOptions<GraniteDataContextPostgres> options)
        : base(options) { }
}
