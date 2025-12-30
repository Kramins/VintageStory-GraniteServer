using System;
using GraniteServer.Data.Entities;
using GraniteServerMod.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace GraniteServerMod.Data;

public class GraniteDataContext : DbContext
{
    public DbSet<ServerEntity> Servers { get; set; } = null!;
    public DbSet<PlayerEntity> Players { get; set; } = null!;
    public DbSet<PlayerSessionEntity> PlayerSessions { get; set; } = null!;
    public DbSet<ModEntity> Mods { get; set; } = null!;
    public DbSet<ModReleaseEntity> ModReleases { get; set; } = null!;

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

        modelBuilder.Entity<PlayerEntity>(entity =>
        {
            entity.HasKey(e => new { e.Id, e.ServerId });
            entity.Property(e => e.Name).IsRequired().HasMaxLength(255);
            entity.Property(e => e.FirstJoinDate).IsRequired();
            entity.Property(e => e.LastJoinDate).IsRequired();
        });

        modelBuilder.Entity<ModEntity>(entity =>
        {
            entity.HasKey(e => e.ModId);
            entity.HasIndex(e => e.ModIdStr).IsUnique();
            entity.Property(e => e.ModIdStr).IsRequired().HasMaxLength(255);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(500);
            entity.Property(e => e.Author).HasMaxLength(255);
            entity.Property(e => e.UrlAlias).HasMaxLength(255);
            entity.Property(e => e.Side).HasMaxLength(50);
            entity.Property(e => e.Type).HasMaxLength(50);
            entity
                .Property(e => e.Tags)
                .HasConversion(
                    v =>
                        System.Text.Json.JsonSerializer.Serialize(
                            v,
                            (System.Text.Json.JsonSerializerOptions?)null
                        ),
                    v =>
                        System.Text.Json.JsonSerializer.Deserialize<List<string>>(
                            v,
                            (System.Text.Json.JsonSerializerOptions?)null
                        ) ?? new List<string>()
                );
            entity.Property(e => e.LastChecked).IsRequired();
        });

        modelBuilder.Entity<ModReleaseEntity>(entity =>
        {
            entity.HasKey(e => e.ReleaseId);
            entity.HasIndex(e => e.ModId);
            entity.Property(e => e.ModId).IsRequired();
            entity.Property(e => e.Filename).HasMaxLength(500);
            entity.Property(e => e.ModIdStr).HasMaxLength(255);
            entity.Property(e => e.ModVersion).HasMaxLength(50);
            entity
                .Property(e => e.Tags)
                .HasConversion(
                    v =>
                        System.Text.Json.JsonSerializer.Serialize(
                            v,
                            (System.Text.Json.JsonSerializerOptions?)null
                        ),
                    v =>
                        System.Text.Json.JsonSerializer.Deserialize<List<string>>(
                            v,
                            (System.Text.Json.JsonSerializerOptions?)null
                        ) ?? new List<string>()
                );

            entity
                .HasOne(e => e.Mod)
                .WithMany(m => m.Releases)
                .HasForeignKey(e => e.ModId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    public override void Dispose()
    {
        base.Dispose();
    }
}
