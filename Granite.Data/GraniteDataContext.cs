using System;
using System.Linq;
using GraniteServer.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace GraniteServer.Data;

public class GraniteDataContext : DbContext
{
    public DbSet<ServerEntity> Servers { get; set; } = null!;
    public DbSet<PlayerEntity> Players { get; set; } = null!;
    public DbSet<PlayerSessionEntity> PlayerSessions { get; set; } = null!;
    public DbSet<PlayerInventorySlotEntity> PlayerInventorySlots { get; set; } = null!;
    public DbSet<CollectibleEntity> Collectibles { get; set; } = null!;
    public DbSet<ServerMetricsEntity> ServerMetrics { get; set; } = null!;
    public DbSet<ModEntity> Mods { get; set; } = null!;
    public DbSet<ModReleaseEntity> ModReleases { get; set; } = null!;
    public DbSet<ModServerEntity> ModServers { get; set; } = null!;
    public DbSet<CommandEntity> BufferedCommands { get; set; } = null!;

    public GraniteDataContext(DbContextOptions options)
        : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        var listStringComparer = new ValueComparer<List<string>>(
            (c1, c2) =>
                (c1 == null && c2 == null) || (c1 != null && c2 != null && c1.SequenceEqual(c2)),
            c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v != null ? v.GetHashCode() : 0)),
            c => c == null ? new List<string>() : c.ToList()
        );

        modelBuilder.Entity<ServerEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(255);
            entity.Property(e => e.AccessToken).IsRequired().HasMaxLength(500);
            entity.Property(e => e.IsOnline).IsRequired();
            entity.Property(e => e.LastSeenAt);
        });

        modelBuilder.Entity<PlayerEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.PlayerUID, e.ServerId }).IsUnique();
            entity.Property(e => e.Name).IsRequired().HasMaxLength(255);
            entity.Property(e => e.FirstJoinDate).IsRequired();
            entity.Property(e => e.LastJoinDate).IsRequired();
            
            entity
                .HasOne(e => e.Server)
                .WithMany(s => s.Players)
                .HasForeignKey(e => e.ServerId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PlayerSessionEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.PlayerId, e.ServerId });
            entity.Property(e => e.PlayerId).IsRequired();
            entity.Property(e => e.IpAddress).IsRequired();
            entity.Property(e => e.PlayerName).IsRequired();
            entity.Property(e => e.JoinDate).IsRequired();

            entity
                .HasOne(e => e.Player)
                .WithMany(p => p.Sessions)
                .HasForeignKey(e => e.PlayerId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PlayerInventorySlotEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.PlayerId, e.ServerId, e.InventoryName, e.SlotIndex }).IsUnique();
            entity.Property(e => e.PlayerId).IsRequired();
            entity.Property(e => e.ServerId).IsRequired();
            entity.Property(e => e.InventoryName).IsRequired().HasMaxLength(255);
            entity.Property(e => e.SlotIndex).IsRequired();
            entity.Property(e => e.EntityId).IsRequired();
            entity.Property(e => e.StackSize).IsRequired();
            entity.Property(e => e.LastUpdated).IsRequired();

            entity
                .HasOne(e => e.Player)
                .WithMany(p => p.InventorySlots)
                .HasForeignKey(e => e.PlayerId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<CollectibleEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            // entity.HasIndex(e => new { e.ServerId, e.CollectibleId }).IsUnique(); // CollectibleId is not unique, we need another property
            entity.Property(e => e.ServerId).IsRequired();
            entity.Property(e => e.CollectibleId).IsRequired();
            entity.Property(e => e.Name).IsRequired().HasMaxLength(500);
            entity.Property(e => e.Type).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Class).HasMaxLength(500);
            entity.Property(e => e.MaxStackSize).IsRequired();
            entity.Property(e => e.LastSynced).IsRequired();

            entity
                .HasOne(e => e.Server)
                .WithMany(s => s.Collectibles)
                .HasForeignKey(e => e.ServerId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ServerMetricsEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.ServerId, e.RecordedAt }).IsUnique(false);
            entity.Property(e => e.RecordedAt).IsRequired();
            entity.Property(e => e.CpuUsagePercent).IsRequired();
            entity.Property(e => e.MemoryUsageMB).IsRequired();
            entity.Property(e => e.ActivePlayerCount).IsRequired();
            
            entity
                .HasOne(e => e.Server)
                .WithMany(s => s.ServerMetrics)
                .HasForeignKey(e => e.ServerId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ModEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.ModId).IsUnique();
            entity.HasIndex(e => e.ModIdStr).IsUnique();
            entity.Property(e => e.ModIdStr).IsRequired().HasMaxLength(255);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(500);
            entity.Property(e => e.Author).HasMaxLength(255);
            entity.Property(e => e.UrlAlias).HasMaxLength(255);
            entity.Property(e => e.Side).HasMaxLength(50);
            entity.Property(e => e.Type).HasMaxLength(50);
            var tagsProp = entity
                .Property(e => e.Tags)
                .HasConversion(
                    v => v == null ? string.Empty : string.Join(",", v),
                    v =>
                        string.IsNullOrWhiteSpace(v)
                            ? new List<string>()
                            : v.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                .Select(s => s.Trim())
                                .ToList()
                );
            tagsProp.Metadata.SetValueComparer(listStringComparer);
            entity.Property(e => e.LastChecked).IsRequired();
        });

        modelBuilder.Entity<ModReleaseEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.ReleaseId).IsUnique();
            entity.HasIndex(e => e.ModId);
            entity.Property(e => e.ModId).IsRequired();
            entity.Property(e => e.Filename).HasMaxLength(500);
            entity.Property(e => e.ModIdStr).HasMaxLength(255);
            entity.Property(e => e.ModVersion).HasMaxLength(50);
            var releaseTagsProp = entity
                .Property(e => e.Tags)
                .HasConversion(
                    v => v == null ? string.Empty : string.Join(",", v),
                    v =>
                        string.IsNullOrWhiteSpace(v)
                            ? new List<string>()
                            : v.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                .Select(s => s.Trim())
                                .ToList()
                );
            releaseTagsProp.Metadata.SetValueComparer(listStringComparer);

            entity
                .HasOne(e => e.Mod)
                .WithMany(m => m.Releases)
                .HasForeignKey(e => e.ModId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ModServerEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.ServerId, e.ModId }).IsUnique();
            entity.Property(e => e.ServerId).IsRequired();
            entity.Property(e => e.ModId).IsRequired();
            entity.Property(e => e.InstalledReleaseId);
            entity.Property(e => e.RunningReleaseId);

            entity
                .HasOne(e => e.Server)
                .WithMany(s => s.ModServers)
                .HasForeignKey(e => e.ServerId)
                .OnDelete(DeleteBehavior.Cascade);

            entity
                .HasOne(e => e.Mod)
                .WithMany(m => m.ModServers)
                .HasForeignKey(e => e.ModId)
                .OnDelete(DeleteBehavior.Cascade);

            entity
                .HasOne(e => e.InstalledRelease)
                .WithMany()
                .HasForeignKey(e => e.InstalledReleaseId)
                .OnDelete(DeleteBehavior.Restrict);

            entity
                .HasOne(e => e.RunningRelease)
                .WithMany()
                .HasForeignKey(e => e.RunningReleaseId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<CommandEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.ServerId, e.Status, e.CreatedAt });
            entity.Property(e => e.ServerId).IsRequired();
            entity.Property(e => e.MessageType).IsRequired().HasMaxLength(255);
            entity.Property(e => e.Payload).IsRequired();
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.Status).IsRequired();
            
            entity
                .HasOne(e => e.Server)
                .WithMany()
                .HasForeignKey(e => e.ServerId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
