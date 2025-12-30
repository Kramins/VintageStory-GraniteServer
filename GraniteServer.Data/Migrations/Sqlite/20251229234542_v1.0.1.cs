using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GraniteServer.Data.Migrations.Sqlite
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
                    ModId = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
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
                    table.PrimaryKey("PK_Mods", x => x.ModId);
                });

            migrationBuilder.CreateTable(
                name: "ModReleases",
                columns: table => new
                {
                    ReleaseId = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ModId = table.Column<long>(type: "INTEGER", nullable: false),
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
                    table.PrimaryKey("PK_ModReleases", x => x.ReleaseId);
                    table.ForeignKey(
                        name: "FK_ModReleases_Mods_ModId",
                        column: x => x.ModId,
                        principalTable: "Mods",
                        principalColumn: "ModId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ModReleases_ModId",
                table: "ModReleases",
                column: "ModId");

            migrationBuilder.CreateIndex(
                name: "IX_Mods_ModIdStr",
                table: "Mods",
                column: "ModIdStr",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ModReleases");

            migrationBuilder.DropTable(
                name: "Mods");
        }
    }
}
