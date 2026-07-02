using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace App.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddScrapeRuns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ScrapeRuns",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    StartedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FinishedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Trigger = table.Column<string>(type: "text", nullable: false),
                    TotalSources = table.Column<int>(type: "integer", nullable: false),
                    SuccessfulSources = table.Column<int>(type: "integer", nullable: false),
                    EventsFetched = table.Column<int>(type: "integer", nullable: false),
                    EventsSaved = table.Column<int>(type: "integer", nullable: false),
                    EventsDeleted = table.Column<int>(type: "integer", nullable: false),
                    EventsEmbedded = table.Column<int>(type: "integer", nullable: false),
                    EmbeddingFailures = table.Column<int>(type: "integer", nullable: false),
                    EventsReclassified = table.Column<int>(type: "integer", nullable: false),
                    Error = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScrapeRuns", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ScrapeRunSources",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ScrapeRunId = table.Column<int>(type: "integer", nullable: false),
                    SourceName = table.Column<string>(type: "text", nullable: false),
                    Success = table.Column<bool>(type: "boolean", nullable: false),
                    EventsFetched = table.Column<int>(type: "integer", nullable: false),
                    EventsSaved = table.Column<int>(type: "integer", nullable: false),
                    DurationSeconds = table.Column<double>(type: "double precision", nullable: false),
                    Error = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScrapeRunSources", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ScrapeRunSources_ScrapeRuns_ScrapeRunId",
                        column: x => x.ScrapeRunId,
                        principalTable: "ScrapeRuns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ScrapeRuns_StartedAtUtc",
                table: "ScrapeRuns",
                column: "StartedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_ScrapeRunSources_ScrapeRunId",
                table: "ScrapeRunSources",
                column: "ScrapeRunId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ScrapeRunSources");

            migrationBuilder.DropTable(
                name: "ScrapeRuns");
        }
    }
}
