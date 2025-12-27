using System;
using GraniteServerMod.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace GraniteServerMod.Data;

public class GraniteDataContext : DbContext
{
    public DbSet<ServerEntity> Servers { get; set; } = null!;
    public DbSet<PlayerEntity> Players { get; set; } = null!;
    public DbSet<PlayerSessionEntity> PlayerSessions { get; set; } = null!;

    public GraniteDataContext(DbContextOptions options)
        : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<ServerEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(255);
        });
    }
}
