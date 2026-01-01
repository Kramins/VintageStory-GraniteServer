using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GraniteServer.Data.Migrations.Postgres
{
    /// <inheritdoc />
    public partial class v101 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Mods",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ModId = table.Column<long>(type: "bigint", nullable: false),
                    ModIdStr = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Name = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Text = table.Column<string>(type: "text", nullable: true),
                    Author = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    UrlAlias = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    LogoFilename = table.Column<string>(type: "text", nullable: true),
                    LogoFile = table.Column<string>(type: "text", nullable: true),
                    LogoFileDb = table.Column<string>(type: "text", nullable: true),
                    HomePageUrl = table.Column<string>(type: "text", nullable: true),
                    SourceCodeUrl = table.Column<string>(type: "text", nullable: true),
                    TrailerVideoUrl = table.Column<string>(type: "text", nullable: true),
                    IssueTrackerUrl = table.Column<string>(type: "text", nullable: true),
                    WikiUrl = table.Column<string>(type: "text", nullable: true),
                    Downloads = table.Column<int>(type: "integer", nullable: false),
                    Follows = table.Column<int>(type: "integer", nullable: false),
                    TrendingPoints = table.Column<int>(type: "integer", nullable: false),
                    Comments = table.Column<int>(type: "integer", nullable: false),
                    Side = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Created = table.Column<string>(type: "text", nullable: true),
                    LastReleased = table.Column<string>(type: "text", nullable: true),
                    LastModified = table.Column<string>(type: "text", nullable: true),
                    Tags = table.Column<string>(type: "text", nullable: false),
                    LastChecked = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Mods", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ModReleases",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ReleaseId = table.Column<long>(type: "bigint", nullable: false),
                    ModId = table.Column<Guid>(type: "uuid", nullable: false),
                    MainFile = table.Column<string>(type: "text", nullable: true),
                    Filename = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    FileId = table.Column<long>(type: "bigint", nullable: true),
                    Downloads = table.Column<int>(type: "integer", nullable: false),
                    Tags = table.Column<string>(type: "text", nullable: false),
                    ModIdStr = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    ModVersion = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Created = table.Column<string>(type: "text", nullable: true),
                    Changelog = table.Column<string>(type: "text", nullable: true)
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
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ServerId = table.Column<Guid>(type: "uuid", nullable: false),
                    ModId = table.Column<Guid>(type: "uuid", nullable: false),
                    InstalledReleaseId = table.Column<Guid>(type: "uuid", nullable: false),
                    RunningReleaseId = table.Column<Guid>(type: "uuid", nullable: false)
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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ModServers");

            migrationBuilder.DropTable(
                name: "ModReleases");

            migrationBuilder.DropTable(
                name: "Mods");
        }
    }
}
