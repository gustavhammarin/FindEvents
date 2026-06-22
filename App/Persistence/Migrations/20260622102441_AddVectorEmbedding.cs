using Microsoft.EntityFrameworkCore.Migrations;
using Pgvector;

#nullable disable

namespace App.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddVectorEmbedding : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:vector", ",,");

            migrationBuilder.AddColumn<Vector>(
                name: "Embedding",
                table: "Events",
                type: "vector(1024)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Events_Embedding",
                table: "Events",
                column: "Embedding")
                .Annotation("Npgsql:CreatedConcurrently", true)
                .Annotation("Npgsql:IndexMethod", "hnsw")
                .Annotation("Npgsql:IndexOperators", new[] { "vector_cosine_ops" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Events_Embedding",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "Embedding",
                table: "Events");

            migrationBuilder.AlterDatabase()
                .OldAnnotation("Npgsql:PostgresExtension:vector", ",,");
        }
    }
}
