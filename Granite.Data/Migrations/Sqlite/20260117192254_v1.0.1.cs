using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Granite.Data.Migrations.Sqlite
{
    /// <inheritdoc />
    public partial class v101 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AccessToken",
                table: "Servers",
                type: "TEXT",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

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
                    Created = table.Column<string>(type: "TEXT", nullable: true),
                    LastReleased = table.Column<string>(type: "TEXT", nullable: true),
                    LastModified = table.Column<string>(type: "TEXT", nullable: true),
                    Tags = table.Column<string>(type: "TEXT", nullable: false),
                    LastChecked = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Mods", x => x.Id);
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
                    ActivePlayerCount = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServerMetrics", x => x.Id);
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
                    Created = table.Column<string>(type: "TEXT", nullable: true),
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
                name: "IX_ServerMetrics_ServerId_RecordedAt",
                table: "ServerMetrics",
                columns: new[] { "ServerId", "RecordedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ModServers");

            migrationBuilder.DropTable(
                name: "ServerMetrics");

            migrationBuilder.DropTable(
                name: "ModReleases");

            migrationBuilder.DropTable(
                name: "Mods");

            migrationBuilder.DropColumn(
                name: "AccessToken",
                table: "Servers");
        }
    }
}
