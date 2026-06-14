using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistence.Migrations
{
    public partial class AddPgTrgmSearch : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("CREATE EXTENSION IF NOT EXISTS pg_trgm;");

            migrationBuilder.Sql("""
                CREATE INDEX IX_Events_Title_Trgm ON "Events" USING GIN ("Title" gin_trgm_ops);
                """);

            migrationBuilder.Sql("""
                CREATE INDEX IX_Events_Description_Trgm ON "Events" USING GIN ("Description" gin_trgm_ops);
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""DROP INDEX IF EXISTS IX_Events_Title_Trgm;""");
            migrationBuilder.Sql("""DROP INDEX IF EXISTS IX_Events_Description_Trgm;""");
        }
    }
}
