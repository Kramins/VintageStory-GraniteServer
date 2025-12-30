using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

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
                    ModId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
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
                    table.PrimaryKey("PK_Mods", x => x.ModId);
                });

            migrationBuilder.CreateTable(
                name: "ModReleases",
                columns: table => new
                {
                    ReleaseId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ModId = table.Column<long>(type: "bigint", nullable: false),
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
