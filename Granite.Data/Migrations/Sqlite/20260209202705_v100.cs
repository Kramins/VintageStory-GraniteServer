using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Granite.Data.Migrations.Sqlite
{
    /// <inheritdoc />
    public partial class v100 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Mods",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ModId = table.Column<long>(type: "INTEGER", nullable: false),
                    ModIdStr = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    Text = table.Column<string>(type: "TEXT", nullable: true),
                    Author = table.Column<string>(type: "TEXT", maxLength: 255, nullable: true),
                    UrlAlias = table.Column<string>(type: "TEXT", maxLength: 255, nullable: true),
                    LogoFilename = table.Column<string>(type: "TEXT", nullable: true),
                    LogoFile = table.Column<string>(type: "TEXT", nullable: true),
                    LogoFileDb = table.Column<string>(type: "TEXT", nullable: true),
                    HomePageUrl = table.Column<string>(type: "TEXT", nullable: true),
                    SourceCodeUrl = table.Column<string>(type: "TEXT", nullable: true),
                    TrailerVideoUrl = table.Column<string>(type: "TEXT", nullable: true),
                    IssueTrackerUrl = table.Column<string>(type: "TEXT", nullable: true),
                    WikiUrl = table.Column<string>(type: "TEXT", nullable: true),
                    Downloads = table.Column<int>(type: "INTEGER", nullable: false),
                    Follows = table.Column<int>(type: "INTEGER", nullable: false),
                    TrendingPoints = table.Column<int>(type: "INTEGER", nullable: false),
                    Comments = table.Column<int>(type: "INTEGER", nullable: false),
                    Side = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    Type = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    Created = table.Column<DateTime>(type: "TEXT", nullable: true),
                    LastReleased = table.Column<DateTime>(type: "TEXT", nullable: true),
                    LastModified = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Tags = table.Column<string>(type: "TEXT", nullable: false),
                    LastChecked = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Mods", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Servers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    AccessToken = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastSeenAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    IsOnline = table.Column<bool>(type: "INTEGER", nullable: false),
                    Port = table.Column<int>(type: "INTEGER", nullable: true),
                    WelcomeMessage = table.Column<string>(type: "TEXT", nullable: true),
                    MaxClients = table.Column<int>(type: "INTEGER", nullable: true),
                    Password = table.Column<string>(type: "TEXT", nullable: true),
                    MaxChunkRadius = table.Column<int>(type: "INTEGER", nullable: true),
                    WhitelistMode = table.Column<string>(type: "TEXT", nullable: true),
                    AllowPvP = table.Column<bool>(type: "INTEGER", nullable: true),
                    AllowFireSpread = table.Column<bool>(type: "INTEGER", nullable: true),
                    AllowFallingBlocks = table.Column<bool>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Servers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ModReleases",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ReleaseId = table.Column<long>(type: "INTEGER", nullable: false),
                    ModId = table.Column<Guid>(type: "TEXT", nullable: false),
                    MainFile = table.Column<string>(type: "TEXT", nullable: true),
                    Filename = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    FileId = table.Column<long>(type: "INTEGER", nullable: true),
                    Downloads = table.Column<int>(type: "INTEGER", nullable: false),
                    Tags = table.Column<string>(type: "TEXT", nullable: false),
                    ModIdStr = table.Column<string>(type: "TEXT", maxLength: 255, nullable: true),
                    ModVersion = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    Created = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Changelog = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ModReleases", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ModReleases_Mods_ModId",
                        column: x => x.ModId,
                        principalTable: "Mods",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BufferedCommands",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ServerId = table.Column<Guid>(type: "TEXT", nullable: false),
                    MessageType = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    Payload = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    SentAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    ResponsePayload = table.Column<string>(type: "TEXT", nullable: true),
                    ErrorMessage = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BufferedCommands", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BufferedCommands_Servers_ServerId",
                        column: x => x.ServerId,
                        principalTable: "Servers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Collectibles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ServerId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CollectibleId = table.Column<int>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    Type = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    MaxStackSize = table.Column<int>(type: "INTEGER", nullable: false),
                    Class = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    Domain = table.Column<string>(type: "TEXT", nullable: false),
                    Path = table.Column<string>(type: "TEXT", nullable: false),
                    BlockMaterial = table.Column<string>(type: "TEXT", nullable: false),
                    MapColorCode = table.Column<string>(type: "TEXT", nullable: false),
                    LastSynced = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Collectibles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Collectibles_Servers_ServerId",
                        column: x => x.ServerId,
                        principalTable: "Servers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MapChunks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ServerId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ChunkX = table.Column<int>(type: "INTEGER", nullable: false),
                    ChunkZ = table.Column<int>(type: "INTEGER", nullable: false),
                    ContentHash = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    RainHeightMapData = table.Column<string>(type: "TEXT", nullable: false),
                    SurfaceBlockIdsData = table.Column<string>(type: "TEXT", nullable: false),
                    ExtractedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ReceivedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastAccessedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MapChunks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MapChunks_Servers_ServerId",
                        column: x => x.ServerId,
                        principalTable: "Servers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Players",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    PlayerUID = table.Column<string>(type: "TEXT", nullable: false),
                    ServerId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    FirstJoinDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastJoinDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    IsWhitelisted = table.Column<bool>(type: "INTEGER", nullable: false),
                    WhitelistedReason = table.Column<string>(type: "TEXT", nullable: true),
                    WhitelistedBy = table.Column<string>(type: "TEXT", nullable: true),
                    IsBanned = table.Column<bool>(type: "INTEGER", nullable: false),
                    BanReason = table.Column<string>(type: "TEXT", nullable: true),
                    BanBy = table.Column<string>(type: "TEXT", nullable: true),
                    BanUntil = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Players", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Players_Servers_ServerId",
                        column: x => x.ServerId,
                        principalTable: "Servers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ServerMetrics",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ServerId = table.Column<Guid>(type: "TEXT", nullable: false),
                    RecordedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CpuUsagePercent = table.Column<float>(type: "REAL", nullable: false),
                    MemoryUsageMB = table.Column<float>(type: "REAL", nullable: false),
                    ActivePlayerCount = table.Column<int>(type: "INTEGER", nullable: false),
                    UpTimeSeconds = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServerMetrics", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ServerMetrics_Servers_ServerId",
                        column: x => x.ServerId,
                        principalTable: "Servers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ModServers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ServerId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ModId = table.Column<Guid>(type: "TEXT", nullable: false),
                    InstalledReleaseId = table.Column<Guid>(type: "TEXT", nullable: true),
                    RunningReleaseId = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ModServers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ModServers_ModReleases_InstalledReleaseId",
                        column: x => x.InstalledReleaseId,
                        principalTable: "ModReleases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ModServers_ModReleases_RunningReleaseId",
                        column: x => x.RunningReleaseId,
                        principalTable: "ModReleases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ModServers_Mods_ModId",
                        column: x => x.ModId,
                        principalTable: "Mods",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ModServers_Servers_ServerId",
                        column: x => x.ServerId,
                        principalTable: "Servers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PlayerInventorySlots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    PlayerId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ServerId = table.Column<Guid>(type: "TEXT", nullable: false),
                    InventoryName = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    SlotIndex = table.Column<int>(type: "INTEGER", nullable: false),
                    EntityId = table.Column<int>(type: "INTEGER", nullable: false),
                    EntityClass = table.Column<string>(type: "TEXT", nullable: true),
                    Name = table.Column<string>(type: "TEXT", nullable: true),
                    StackSize = table.Column<int>(type: "INTEGER", nullable: false),
                    LastUpdated = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayerInventorySlots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlayerInventorySlots_Players_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PlayerSessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    PlayerId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ServerId = table.Column<Guid>(type: "TEXT", nullable: false),
                    JoinDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    IpAddress = table.Column<string>(type: "TEXT", nullable: false),
                    PlayerName = table.Column<string>(type: "TEXT", nullable: false),
                    LeaveDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Duration = table.Column<double>(type: "REAL", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayerSessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlayerSessions_Players_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BufferedCommands_ServerId_Status_CreatedAt",
                table: "BufferedCommands",
                columns: new[] { "ServerId", "Status", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Collectibles_ServerId",
                table: "Collectibles",
                column: "ServerId");

            migrationBuilder.CreateIndex(
                name: "IX_MapChunks_LastAccessedAt",
                table: "MapChunks",
                column: "LastAccessedAt");

            migrationBuilder.CreateIndex(
                name: "IX_MapChunks_ServerId_ChunkX_ChunkZ",
                table: "MapChunks",
                columns: new[] { "ServerId", "ChunkX", "ChunkZ" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MapChunks_ServerId_ContentHash",
                table: "MapChunks",
                columns: new[] { "ServerId", "ContentHash" });

            migrationBuilder.CreateIndex(
                name: "IX_ModReleases_ModId",
                table: "ModReleases",
                column: "ModId");

            migrationBuilder.CreateIndex(
                name: "IX_ModReleases_ReleaseId",
                table: "ModReleases",
                column: "ReleaseId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Mods_ModId",
                table: "Mods",
                column: "ModId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Mods_ModIdStr",
                table: "Mods",
                column: "ModIdStr",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ModServers_InstalledReleaseId",
                table: "ModServers",
                column: "InstalledReleaseId");

            migrationBuilder.CreateIndex(
                name: "IX_ModServers_ModId",
                table: "ModServers",
                column: "ModId");

            migrationBuilder.CreateIndex(
                name: "IX_ModServers_RunningReleaseId",
                table: "ModServers",
                column: "RunningReleaseId");

            migrationBuilder.CreateIndex(
                name: "IX_ModServers_ServerId_ModId",
                table: "ModServers",
                columns: new[] { "ServerId", "ModId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PlayerInventorySlots_PlayerId_ServerId_InventoryName_SlotIndex",
                table: "PlayerInventorySlots",
                columns: new[] { "PlayerId", "ServerId", "InventoryName", "SlotIndex" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Players_PlayerUID_ServerId",
                table: "Players",
                columns: new[] { "PlayerUID", "ServerId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Players_ServerId",
                table: "Players",
                column: "ServerId");

            migrationBuilder.CreateIndex(
                name: "IX_PlayerSessions_PlayerId_ServerId",
                table: "PlayerSessions",
                columns: new[] { "PlayerId", "ServerId" });

            migrationBuilder.CreateIndex(
                name: "IX_ServerMetrics_ServerId_RecordedAt",
                table: "ServerMetrics",
                columns: new[] { "ServerId", "RecordedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Servers_Name",
                table: "Servers",
                column: "Name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BufferedCommands");

            migrationBuilder.DropTable(
                name: "Collectibles");

            migrationBuilder.DropTable(
                name: "MapChunks");

            migrationBuilder.DropTable(
                name: "ModServers");

            migrationBuilder.DropTable(
                name: "PlayerInventorySlots");

            migrationBuilder.DropTable(
                name: "PlayerSessions");

            migrationBuilder.DropTable(
                name: "ServerMetrics");

            migrationBuilder.DropTable(
                name: "ModReleases");

            migrationBuilder.DropTable(
                name: "Players");

            migrationBuilder.DropTable(
                name: "Mods");

            migrationBuilder.DropTable(
                name: "Servers");
        }
    }
}
